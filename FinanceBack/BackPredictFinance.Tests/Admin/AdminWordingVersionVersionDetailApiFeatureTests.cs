using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using BackPredictFinance.Common.enums;
using BackPredictFinance.ViewModels.AdminViewModels.Wording;

using BackPredictFinance.Tests.Infrastructure;

namespace BackPredictFinance.Tests.Admin;

public sealed class AdminWordingVersionVersionDetailApiFeatureTests : IClassFixture<ApiIntegrationTestFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = CreateJsonOptions();
    private readonly ApiIntegrationTestFactory _factory;

    public AdminWordingVersionVersionDetailApiFeatureTests(ApiIntegrationTestFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetVersionDetail_ReturnsForbidden_ForStandardUser()
    {
        var client = _factory.CreateAuthenticatedClient(ApiIntegrationTestFactory.StandardUserId, UserRoleEnum.User);

        var response = await client.GetAsync("/api/admin/wording-versions/REC_WORDING_V1");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetVersionDetail_ReturnsGovernedVersionProjection_ForAdmin()
    {
        var client = _factory.CreateAuthenticatedClient(ApiIntegrationTestFactory.AdminUserId, UserRoleEnum.Admin);

        var response = await client.GetAsync("/api/admin/wording-versions/REC_WORDING_V1");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<AdminWordingVersionScenariosViewModel>(JsonOptions);
        Assert.NotNull(payload);
        Assert.Equal("REC_WORDING_V1", payload!.WordingVersionId);
        Assert.Equal("PredictFinance V1 recommendation wording", payload.DisplayName);
        Assert.True(payload.PublicationState.IsActive);
        Assert.NotEmpty(payload.Scenarios);
        Assert.Contains(payload.Scenarios, scenario => scenario.ScenarioCode == "HELD_REINFORCE");
    }

    private static JsonSerializerOptions CreateJsonOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }
}
