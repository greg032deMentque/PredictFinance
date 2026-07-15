using System.Net.Http.Headers;
using System.Security.Claims;
using BackPredictFinance.Common.enums;
using BackPredictFinance.Datas.Context;
using BackPredictFinance.Datas.Entities;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BackPredictFinance.Tests.Infrastructure;

public sealed class ApiIntegrationTestFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public const string StandardUserId = "it-user-1";
    public const string AdminUserId = "it-admin-1";
    public const string TargetUserId = "it-target-1";

    private const string JwtIssuer = "PredictFinance.Tests";
    private const string JwtAudience = "PredictFinance.Tests.Client";
    private const string JwtSecret = "PredictFinance.Tests.Secret.Key.For.Jwt.123456";
    private readonly string _databaseName = $"predictfinance-it-{Guid.NewGuid():N}";
    private readonly IServiceProvider _inMemoryServiceProvider = new ServiceCollection()
        .AddEntityFrameworkInMemoryDatabase()
        .BuildServiceProvider();

    static ApiIntegrationTestFactory()
    {
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Testing");
        Environment.SetEnvironmentVariable("DOTNET_ENVIRONMENT", "Testing");
    }

    protected override void ConfigureWebHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, configBuilder) =>
        {
            configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = "Server=(localdb)\\mssqllocaldb;Database=PredictFinanceTesting;Trusted_Connection=True;TrustServerCertificate=True;",
                ["JWTToken:Issuer"] = JwtIssuer,
                ["JWTToken:Audience"] = JwtAudience,
                ["JWTToken:Secret"] = JwtSecret,
                ["JWTToken:ValidityMinutesAcessToken"] = "15",
                ["JWTToken:ValidityMinutesRefreshToken"] = "1440",
                ["Frontend:BaseUrl"] = "http://localhost:4200"
            });
        });

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<FinanceDbContext>>();
            services.RemoveAll<FinanceDbContext>();
            services.AddDbContext<FinanceDbContext>(options =>
            {
                options.UseInMemoryDatabase(_databaseName);
                options.UseInternalServiceProvider(_inMemoryServiceProvider);
            });

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = TestAuthHandler.SchemeName;
                options.DefaultChallengeScheme = TestAuthHandler.SchemeName;
            }).AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, _ => { });

            services.PostConfigure<AuthorizationOptions>(options =>
            {
                options.AddPolicy("Bearer", policy =>
                {
                    policy.AddAuthenticationSchemes(TestAuthHandler.SchemeName);
                    policy.RequireAuthenticatedUser();
                });

                options.AddPolicy("RequireAdminRole", policy =>
                {
                    policy.AddAuthenticationSchemes(TestAuthHandler.SchemeName);
                    policy.RequireRole(UserRoleEnum.Admin.ToString());
                });
            });
        });
    }

    public async Task InitializeAsync()
    {
        await SeedIdentityAsync();
    }

    Task IAsyncLifetime.DisposeAsync() => Task.CompletedTask;

    public HttpClient CreateAnonymousClient()
    {
        return CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    public HttpClient CreateAuthenticatedClient(string userId, params UserRoleEnum[] roles)
    {
        var client = CreateAnonymousClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(TestAuthHandler.SchemeName, "integration-test");
        client.DefaultRequestHeaders.Add(TestAuthHandler.UserIdHeaderName, userId);
        client.DefaultRequestHeaders.Add(TestAuthHandler.RolesHeaderName, string.Join(',', roles.Select(role => role.ToString())));
        return client;
    }

    private async Task SeedIdentityAsync()
    {
        using var scope = Services.CreateScope();
        var financeDbContext = scope.ServiceProvider.GetRequiredService<FinanceDbContext>();
        await financeDbContext.Database.EnsureCreatedAsync();

        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        foreach (var roleName in Enum.GetNames(typeof(UserRoleEnum)))
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                await roleManager.CreateAsync(new IdentityRole(roleName));
            }
        }

        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
        await EnsureUserAsync(
            userManager,
            StandardUserId,
            "user@example.com",
            "Marie",
            "User",
            UserRoleEnum.User);
        await EnsureUserAsync(
            userManager,
            AdminUserId,
            "admin@example.com",
            "Alice",
            "Admin",
            UserRoleEnum.Admin);
        await EnsureUserAsync(
            userManager,
            TargetUserId,
            "target@example.com",
            "Theo",
            "Target",
            UserRoleEnum.User);
    }

    private static async Task EnsureUserAsync(
        UserManager<User> userManager,
        string userId,
        string email,
        string firstName,
        string lastName,
        UserRoleEnum role)
    {
        var existingUser = await userManager.FindByIdAsync(userId);
        if (existingUser is not null)
        {
            return;
        }

        var user = new User
        {
            Id = userId,
            UserName = email,
            Email = email,
            FirstName = firstName,
            LastName = lastName,
            IsActive = true,
            RefreshToken = string.Empty
        };

        var createResult = await userManager.CreateAsync(user, "Password1");
        if (!createResult.Succeeded)
        {
            throw new InvalidOperationException(string.Join(" | ", createResult.Errors.Select(error => error.Description)));
        }

        var roleResult = await userManager.AddToRoleAsync(user, role.ToString());
        if (!roleResult.Succeeded)
        {
            throw new InvalidOperationException(string.Join(" | ", roleResult.Errors.Select(error => error.Description)));
        }
    }
}

internal sealed class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string SchemeName = "TestAuth";
    public const string UserIdHeaderName = "X-Test-UserId";
    public const string RolesHeaderName = "X-Test-Roles";

    public TestAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        System.Text.Encodings.Web.UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(UserIdHeaderName, out var userIdValues))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var userId = userIdValues.ToString().Trim();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var claims = new List<Claim>
        {
            new("sub", userId),
            new(ClaimTypes.NameIdentifier, userId)
        };

        if (Request.Headers.TryGetValue(RolesHeaderName, out var rolesHeaderValues))
        {
            var roles = rolesHeaderValues.ToString()
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }
        }

        var identity = new ClaimsIdentity(claims, SchemeName);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, SchemeName);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
