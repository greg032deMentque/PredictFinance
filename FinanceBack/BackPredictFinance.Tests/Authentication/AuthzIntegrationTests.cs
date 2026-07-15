using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using BackPredictFinance.Common.enums;
using BackPredictFinance.ViewModels.UserViewModels;

using BackPredictFinance.Tests.Infrastructure;

namespace BackPredictFinance.Tests.Authentication;

public sealed class AuthzIntegrationTests : IClassFixture<ApiIntegrationTestFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = CreateJsonOptions();
    private readonly ApiIntegrationTestFactory _factory;

    public AuthzIntegrationTests(ApiIntegrationTestFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task AccountMe_ReturnsUnauthorized_WhenAnonymous()
    {
        var client = _factory.CreateAnonymousClient();

        var response = await client.GetAsync("/api/Account/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task AccountMe_ReturnsBackendGovernedCurrentUserPayload_WhenAuthenticated()
    {
        var client = _factory.CreateAuthenticatedClient(ApiIntegrationTestFactory.StandardUserId, UserRoleEnum.User);

        var response = await client.GetAsync("/api/Account/me");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<CurrentUserViewModel>(JsonOptions);
        Assert.NotNull(payload);
        Assert.Equal(ApiIntegrationTestFactory.StandardUserId, payload!.UserId);
        Assert.Equal("user@example.com", payload.Email);
        Assert.Equal("Marie User", payload.DisplayName);
        Assert.Equal([UserRoleEnum.User], payload.Roles);
        Assert.Equal([UserAreaEnum.User], payload.AllowedAreas);
    }


    [Fact]
    public async Task AccountMe_ReturnsBackendGovernedAdminArea_WhenAdminAuthenticated()
    {
        var client = _factory.CreateAuthenticatedClient(ApiIntegrationTestFactory.AdminUserId, UserRoleEnum.Admin);

        var response = await client.GetAsync("/api/Account/me");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<CurrentUserViewModel>(JsonOptions);
        Assert.NotNull(payload);
        Assert.Equal(ApiIntegrationTestFactory.AdminUserId, payload!.UserId);
        Assert.Equal("admin@example.com", payload.Email);
        Assert.Equal("Alice Admin", payload.DisplayName);
        Assert.Equal([UserRoleEnum.Admin], payload.Roles);
        Assert.Equal([UserAreaEnum.User, UserAreaEnum.Admin], payload.AllowedAreas);
    }

    [Fact]
    public async Task AccountMe_ReturnsStableShellShape()
    {
        var client = _factory.CreateAuthenticatedClient(ApiIntegrationTestFactory.AdminUserId, UserRoleEnum.Admin);

        var response = await client.GetAsync("/api/Account/me");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = document.RootElement;

        Assert.Equal(JsonValueKind.Object, root.ValueKind);
        var propertyNames = root.EnumerateObject().Select(property => property.Name).OrderBy(name => name, StringComparer.Ordinal).ToArray();
        Assert.Equal(["AllowedAreas", "DisplayName", "Email", "Roles", "UserId"], propertyNames);
        Assert.Equal(ApiIntegrationTestFactory.AdminUserId, root.GetProperty("UserId").GetString());
        Assert.Equal("Alice Admin", root.GetProperty("DisplayName").GetString());
        Assert.Equal("admin@example.com", root.GetProperty("Email").GetString());
        Assert.Equal(new string?[] { "Admin" }, root.GetProperty("Roles").EnumerateArray().Select(value => value.GetString()).ToArray());
        Assert.Equal(new string?[] { "User", "Admin" }, root.GetProperty("AllowedAreas").EnumerateArray().Select(value => value.GetString()).ToArray());
    }

    [Fact]
    public async Task AccountUnblockIp_ReturnsForbidden_ForNonAdminUser()
    {
        var client = _factory.CreateAuthenticatedClient(ApiIntegrationTestFactory.StandardUserId, UserRoleEnum.User);

        var response = await client.DeleteAsync("/api/admin/security/ip-blocks/127.0.0.1");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task UserGetUserById_ReturnsForbidden_ForNonAdminUser()
    {
        var client = _factory.CreateAuthenticatedClient(ApiIntegrationTestFactory.StandardUserId, UserRoleEnum.User);

        var response = await client.GetAsync($"/api/admin/users/{ApiIntegrationTestFactory.TargetUserId}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task UserGetUserById_ReturnsRequestedUser_ForAdmin()
    {
        var client = _factory.CreateAuthenticatedClient(ApiIntegrationTestFactory.AdminUserId, UserRoleEnum.Admin);

        var response = await client.GetAsync($"/api/admin/users/{ApiIntegrationTestFactory.TargetUserId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var rawJson = await response.Content.ReadAsStringAsync();
        var payload = JsonSerializer.Deserialize<UserViewModel>(rawJson, JsonOptions);
        Assert.NotNull(payload);
        Assert.Equal(ApiIntegrationTestFactory.TargetUserId, payload!.Id);
        Assert.Equal("target@example.com", payload.Email);

        using var document = JsonDocument.Parse(rawJson);
        Assert.False(document.RootElement.TryGetProperty("Password", out _));
        Assert.False(document.RootElement.TryGetProperty("RefreshToken", out _));
        Assert.False(document.RootElement.TryGetProperty("RefreshTokenExpiryTime", out _));
    }

    private static JsonSerializerOptions CreateJsonOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }
}
