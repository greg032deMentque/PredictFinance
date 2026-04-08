using BackPredictFinance.Services;
using BackPredictFinance.Services.AuthServices;
using BackPredictFinance.Services.ClientFinanceServices;
using BackPredictFinance.Services.ClientFinanceServices.Analysis;
using BackPredictFinance.Services.TwelveDataServices;
using BackPredictFinance.Services.UserServices;
namespace BackPredictFinance.API.ProgramSubFiles
{
    public class ProgramServiceDeclarator
    {
        public static void ServicesDeclarator(IServiceCollection services)
        {
            services.AddScoped<ILogService, LogService>();
            services.AddScoped<IPathService, PathService>();
            services.AddScoped<IEmailService, EmailService>();
            services.AddScoped<IJwtGeneratorService, JwtGeneratorService>();
            services.AddScoped<IAccountService, AccountService>();
            services.AddScoped<IUserAssetService, UserAssetService>();
            services.AddScoped<IUserRoleDataService, UserRoleDataService>();
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<IAssetService, AssetService>();
            services.AddScoped<AnalyticService>();
            services.AddHttpClient<YahooFinanceMarketDataProvider>();
            services.AddTransient<IMarketCatalogProvider>(serviceProvider => serviceProvider.GetRequiredService<YahooFinanceMarketDataProvider>());
            services.AddTransient<IMarketPriceProvider>(serviceProvider => serviceProvider.GetRequiredService<YahooFinanceMarketDataProvider>());
            services.AddTransient<IFundamentalsProvider>(serviceProvider => serviceProvider.GetRequiredService<YahooFinanceMarketDataProvider>());
            services.AddScoped<ITickerService, TickerService>();
            services.AddScoped<ITradingRecommendationService, TradingRecommendationService>();
            services.AddScoped<IAnalysisRequestCompatibilityResolver, AnalysisRequestCompatibilityResolver>();
            services.AddScoped<IAnalysisLegacyCompatibilityService, AnalysisLegacyCompatibilityService>();
            services.AddScoped<IAnalysisPatternDefinition, RectangleContinuationPatternDefinition>();
            services.AddScoped<IAnalysisPatternDefinition, SymmetricalTriangleContinuationPatternDefinition>();
            services.AddScoped<IAnalysisPatternDefinition, BullFlagContinuationPatternDefinition>();
            services.AddScoped<IAnalysisPatternDefinition, BearFlagContinuationPatternDefinition>();
            services.AddScoped<IAnalysisPatternRegistry, AnalysisPatternRegistry>();
            services.AddScoped<IPortfolioContextLoader, PortfolioContextLoader>();
            services.AddScoped<IAnalysisExecutionService, DeterministicAnalysisExecutionService>();
            services.AddScoped<IRiskEvaluationService, RiskEvaluationService>();
            services.AddScoped<IRecommendationPolicyService, RecommendationPolicyService>();
            services.AddScoped<IPedagogicalExplanationService, PedagogicalExplanationService>();
            services.AddScoped<IAnalysisSnapshotPersistenceService, AnalysisSnapshotPersistenceService>();
            services.AddScoped<IAnalysisOrchestrator, ClientAnalysisOrchestrator>();
            services.AddScoped<IClientFinanceService, ClientFinanceService>();
        }
    }
}