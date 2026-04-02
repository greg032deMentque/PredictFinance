using AutoMapper;
using BackPredictFinance.Common;
using BackPredictFinance.Datas.Context;
using BackPredictFinance.Datas.Entities;
using BackPredictFinance.Services;
using BackPredictFinance.Services.ClientFinanceServices.AnalysisV1;
using BackPredictFinance.Services.PythonServices;
using BackPredictFinance.Services.PythonServices.Models;
using BackPredictFinance.ViewModels.ClientFinanceViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using System.Reflection;
using System.Security.Claims;

namespace BackPredictFinance.Tests
{
    public class ClientFinanceServiceTests
    {
        [Fact]
        public async Task AnalysisOrchestrator_RunAnalysisAsync_PersistsFailedAnalysisRun_WhenPythonFails()
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());

            var httpContextAccessor = BuildHttpContextAccessor();
            services.AddSingleton<IHttpContextAccessor>(httpContextAccessor);
            services.AddDbContext<FinanceDbContext>(options =>
                options.UseInMemoryDatabase($"PredictFinanceTests_{Guid.NewGuid()}"));

            var mapperConfiguration = new MapperConfiguration(cfg =>
            {
                cfg.AddMaps(typeof(AnalysisResultViewModelProfile).Assembly);
            }, NullLoggerFactory.Instance);
            services.AddSingleton<IMapper>(mapperConfiguration.CreateMapper());

            services.AddSingleton(Mock.Of<IStringLocalizer<Messages>>());
            services.AddSingleton(CreateUserManagerMock().Object);
            services.AddSingleton(CreateRoleManagerMock().Object);
            services.AddSingleton(sp => CreateSignInManagerMock(sp.GetRequiredService<UserManager<User>>(), httpContextAccessor).Object);
            services.AddSingleton(Mock.Of<ILogService>());

            await using var provider = services.BuildServiceProvider();
            await using var scope = provider.CreateAsyncScope();
            var scopedProvider = scope.ServiceProvider;
            var dbContext = scopedProvider.GetRequiredService<FinanceDbContext>();

            var pythonEnvelope = new PythonCliErrorEnvelope
            {
                SchemaVersion = "1.0",
                Source = "cli",
                Operation = "predict",
                ErrorCode = "artifact_missing",
                ErrorType = "FileNotFoundError",
                Message = "Model file not found: artifacts/double_top/model.joblib",
                UserMessage = "Le modèle IA est indisponible pour le moment.",
                Ticker = "AAPL",
                Pattern = "DOUBLE_TOP",
                LoggedAtUtc = DateTime.UtcNow
            };
            var pythonException = PythonCliErrorHandling.CreateCustomException("predict", "AAPL", "DOUBLE_TOP", pythonEnvelope);

            var pythonAdapter = new Mock<IOptionalPythonAnalysisAdapter>();
            pythonAdapter
                .Setup(service => service.ExecuteAsync(It.IsAny<ResolvedAnalysisRunRequest>(), It.IsAny<ResolvedAnalysisPattern>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(pythonException);
            var recommendationPolicyService = new Mock<IRecommendationPolicyService>();
            var patternRegistry = new Mock<IAnalysisPatternRegistry>();
            patternRegistry
                .Setup(service => service.ResolveRequestedPattern(It.IsAny<string?>()))
                .Returns(new ResolvedAnalysisPattern
                {
                    PatternId = "DOUBLE_TOP",
                    ModelDir = "artifacts/double_top",
                    ModelVersion = "double_top@v1"
                });

            var persistenceService = new AnalysisSnapshotPersistenceService(scopedProvider);
            SetAnalysisHistoryAvailable(persistenceService, true);
            var service = new ClientAnalysisOrchestrator(
                patternRegistry.Object,
                pythonAdapter.Object,
                recommendationPolicyService.Object,
                persistenceService);

            var exception = await Assert.ThrowsAsync<CustomException>(() => service.RunAnalysisAsync(
                new ResolvedAnalysisRunRequest
                {
                    UserId = "user-1",
                    Symbol = "AAPL",
                    RequestedPatternId = "DOUBLE_TOP"
                }));

            Assert.Equal("Le modèle IA est indisponible pour le moment.", exception.FrontMessage);

            var analysisRun = await dbContext.AnalysisRuns.SingleAsync();
            Assert.Equal("Failed", analysisRun.Status);
            Assert.Equal("Le modèle IA est indisponible pour le moment.", analysisRun.ErrorMessage);
            Assert.Contains("\"error_code\":\"artifact_missing\"", analysisRun.RawPayload, StringComparison.Ordinal);
            Assert.Equal(0, await dbContext.DecisionSignals.CountAsync());
            Assert.Equal(0, await dbContext.ModelSnapshots.CountAsync());
            Assert.Equal(0, await dbContext.PatternAssessments.CountAsync());
        }

        private static IHttpContextAccessor BuildHttpContextAccessor()
        {
            var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, "user-1"),
                new Claim("sub", "user-1")
            ], "TestAuth"));

            return new HttpContextAccessor
            {
                HttpContext = new DefaultHttpContext
                {
                    User = claimsPrincipal
                }
            };
        }

        private static Mock<UserManager<User>> CreateUserManagerMock()
        {
            var store = new Mock<IUserStore<User>>();
            return new Mock<UserManager<User>>(
                store.Object,
                Mock.Of<IOptions<IdentityOptions>>(),
                Mock.Of<IPasswordHasher<User>>(),
                Array.Empty<IUserValidator<User>>(),
                Array.Empty<IPasswordValidator<User>>(),
                Mock.Of<ILookupNormalizer>(),
                new IdentityErrorDescriber(),
                Mock.Of<IServiceProvider>(),
                Mock.Of<ILogger<UserManager<User>>>());
        }

        private static Mock<RoleManager<IdentityRole>> CreateRoleManagerMock()
        {
            var store = new Mock<IRoleStore<IdentityRole>>();
            return new Mock<RoleManager<IdentityRole>>(
                store.Object,
                Array.Empty<IRoleValidator<IdentityRole>>(),
                Mock.Of<ILookupNormalizer>(),
                new IdentityErrorDescriber(),
                Mock.Of<ILogger<RoleManager<IdentityRole>>>());
        }

        private static Mock<SignInManager<User>> CreateSignInManagerMock(UserManager<User> userManager, IHttpContextAccessor httpContextAccessor)
        {
            return new Mock<SignInManager<User>>(
                userManager,
                httpContextAccessor,
                Mock.Of<IUserClaimsPrincipalFactory<User>>(),
                Mock.Of<IOptions<IdentityOptions>>(),
                Mock.Of<ILogger<SignInManager<User>>>(),
                Mock.Of<IAuthenticationSchemeProvider>(),
                Mock.Of<IUserConfirmation<User>>());
        }

        private static void SetAnalysisHistoryAvailable(AnalysisSnapshotPersistenceService service, bool value)
        {
            var field = typeof(AnalysisSnapshotPersistenceService).GetField("_analysisHistorySchemaAvailable", BindingFlags.Instance | BindingFlags.NonPublic);
            field!.SetValue(service, value);
        }
    }
}
