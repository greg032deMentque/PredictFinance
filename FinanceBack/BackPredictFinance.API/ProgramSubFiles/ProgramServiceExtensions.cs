using BackPredictFinance.Common;
using BackPredictFinance.Services;
using BackPredictFinance.Services.AdminGovernance;
using BackPredictFinance.Services.AuthServices;
using BackPredictFinance.Services.BackgroundJobs;
using BackPredictFinance.Services.ClientFinanceServices;
using BackPredictFinance.Services.ClientFinanceServices.Alerts;
using BackPredictFinance.Services.ClientFinanceServices.Analysis;
using BackPredictFinance.Services.ClientFinanceServices.Indicators;
using BackPredictFinance.Services.ClientFinanceServices.Patterns;
using BackPredictFinance.Services.ClientFinanceServices.PortfolioMetrics;
using BackPredictFinance.Services.ClientFinanceServices.Fundamentals;
using BackPredictFinance.Services.ClientFinanceServices.Screener;
using BackPredictFinance.Services.ClientFinanceServices.Tax;
using BackPredictFinance.Services.Fundamentals;
using BackPredictFinance.Services.Notifications;
using BackPredictFinance.Services.TwelveDataServices;
using BackPredictFinance.Services.UserServices;
using BackPredictFinance.Patterns;
using BackPredictFinance.Patterns.Abstractions;
using BackPredictFinance.Patterns.Definitions;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Options;
using Polly;
using System.Net;

namespace BackPredictFinance.API.ProgramSubFiles
{
    internal static class ProgramServiceExtensions
    {
        internal static IServiceCollection AddAuthAndUserServices(this IServiceCollection services)
        {
            services.AddScoped<ILogService, LogService>();
            services.AddScoped<IPathService, PathService>();
            services.AddScoped<IEmailService, EmailService>();
            services.AddScoped<IJwtGeneratorService, JwtGeneratorService>();
            services.AddScoped<IAccountService, AccountService>();
            services.AddScoped<IUserPrivacyService, UserPrivacyService>();
            services.AddScoped<ICurrentUserSessionService, CurrentUserSessionService>();
            services.AddScoped<IUserAssetService, UserAssetService>();
            services.AddScoped<IUserRoleDataService, UserRoleDataService>();
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<IUserAdminService, UserAdminService>();
            services.AddScoped<AnalyticService>();
            return services;
        }

        internal static IServiceCollection AddMarketDataProviders(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddSingleton<IYahooCrumbService, YahooCrumbService>();
            services.AddHttpClient<YahooFinanceMarketDataProvider>()
                .AddResilienceHandler("yahoo-finance", (builder, context) =>
                {
                    var opts = context.ServiceProvider.GetRequiredService<IOptions<MarketDataOptions>>().Value;

                    static bool IsTransient(Outcome<HttpResponseMessage> outcome) =>
                        outcome.Exception is HttpRequestException
                        || outcome.Result?.StatusCode is HttpStatusCode.TooManyRequests
                            or HttpStatusCode.ServiceUnavailable
                            or HttpStatusCode.GatewayTimeout;

                    builder.AddRetry(new HttpRetryStrategyOptions
                    {
                        MaxRetryAttempts = opts.ResilienceMaxRetryAttempts,
                        BackoffType = DelayBackoffType.Exponential,
                        UseJitter = true,
                        Delay = TimeSpan.FromSeconds(opts.ResilienceBaseDelaySeconds),
                        ShouldRetryAfterHeader = true,
                        ShouldHandle = args => ValueTask.FromResult(IsTransient(args.Outcome))
                    });

                    builder.AddCircuitBreaker(new HttpCircuitBreakerStrategyOptions
                    {
                        FailureRatio = opts.CircuitBreakerFailureRatio,
                        SamplingDuration = TimeSpan.FromSeconds(opts.CircuitBreakerSamplingSeconds),
                        MinimumThroughput = opts.CircuitBreakerMinimumThroughput,
                        ShouldHandle = args => ValueTask.FromResult(IsTransient(args.Outcome))
                    });

                    builder.AddTimeout(TimeSpan.FromSeconds(opts.HttpAttemptTimeoutSeconds));
                });
            services.AddTransient<IMarketCatalogProvider>(sp => sp.GetRequiredService<YahooFinanceMarketDataProvider>());
            services.AddTransient<IMarketPriceProvider>(sp => sp.GetRequiredService<YahooFinanceMarketDataProvider>());
            services.AddTransient<IFundamentalsProvider>(sp => sp.GetRequiredService<YahooFinanceMarketDataProvider>());
            services.AddTransient<IEtfProfileProvider>(sp => sp.GetRequiredService<YahooFinanceMarketDataProvider>());
            services.AddScoped<ITickerService, TickerService>();
            return services;
        }

        internal static IServiceCollection AddPatternServices(this IServiceCollection services)
        {
            services.AddScoped<IDegradedModeState, DegradedModeState>();
            services.AddScoped<PatternMarketDataProvider>();
            services.AddScoped<IPatternMarketDataProvider, FallbackPatternMarketDataProvider>();
            services.AddScoped<IAnalysisPatternDefinition, RectangleContinuationAnalysisPatternDefinition>();
            services.AddScoped<IAnalysisPatternDefinition, SymmetricalTriangleContinuationAnalysisPatternDefinition>();
            services.AddScoped<IAnalysisPatternDefinition, BullFlagContinuationAnalysisPatternDefinition>();
            services.AddScoped<IAnalysisPatternDefinition, BearFlagContinuationAnalysisPatternDefinition>();
            services.AddScoped<IAnalysisPatternDefinition, DoubleBottomReversalPatternDefinition>();
            services.AddScoped<IAnalysisPatternDefinition, DoubleTopReversalPatternDefinition>();
            services.AddScoped<IAnalysisPatternDefinition, InverseHeadAndShouldersReversalPatternDefinition>();
            services.AddScoped<IAnalysisPatternDefinition, HeadAndShouldersReversalPatternDefinition>();
            services.AddScoped<IAnalysisPatternRegistry, AnalysisPatternRegistry>();
            services.AddScoped<IPatternScenarioBranchGenerator, PatternScenarioBranchGenerator>();
            services.AddScoped<IPatternExplorerService, PatternExplorerService>();
            return services;
        }

        internal static IServiceCollection AddAnalysisServices(this IServiceCollection services)
        {
            services.AddScoped<ITradingRecommendationService, TradingRecommendationService>();
            services.AddScoped<IAnalysisRequestCompatibilityResolver, AnalysisRequestCompatibilityResolver>();
            services.AddScoped<IAnalysisResultProjectionService, AnalysisResultProjectionService>();
            services.AddScoped<IPortfolioContextLoader, PortfolioContextLoader>();
            services.AddScoped<IAnalysisExecutionService, DeterministicAnalysisExecutionService>();
            services.AddScoped<IRiskEvaluationService, RiskEvaluationService>();
            services.AddScoped<IRecommendationPolicyService, RecommendationPolicyService>();
            services.AddScoped<IAnalysisAccompanimentWordingProvider, AnalysisAccompanimentWordingProvider>();
            services.AddScoped<IConfidenceBreakdownAssembler, ConfidenceBreakdownAssembler>();
            services.AddScoped<IActionPlanGenerationService, ActionPlanGenerationService>();
            services.AddScoped<IPedagogicalExplanationService, PedagogicalExplanationService>();
            services.AddScoped<IAnalysisSnapshotPersistenceService, AnalysisSnapshotPersistenceService>();
            services.AddScoped<IAnalysisOrchestrator, ClientAnalysisOrchestrator>();
            services.AddScoped<IClientFinanceSnapshotComparisonService, ClientFinanceSnapshotComparisonService>();
            services.AddScoped<IExPostStatisticsService, ExPostStatisticsService>();
            services.AddScoped<IAnalysisContentService, AnalysisContentService>();
            return services;
        }

        internal static IServiceCollection AddClientFinanceServices(this IServiceCollection services)
        {
            services.AddScoped<IClientFinanceAssetSupportService, ClientFinanceAssetSupportService>();
            services.AddScoped<IClientFinanceProjectionService, ClientFinanceProjectionService>();
            services.AddScoped<IClientFinanceWatchlistPortfolioService, ClientFinanceWatchlistPortfolioService>();
            services.AddScoped<IClientFinanceTransactionService, ClientFinanceTransactionService>();
            services.AddScoped<IClientFinanceDashboardHistoryService, ClientFinanceDashboardHistoryService>();
            services.AddScoped<IClientFinanceHistoryReadService, ClientFinanceHistoryReadService>();
            services.AddScoped<IClientFinanceInstrumentDetailService, ClientFinanceInstrumentDetailService>();
            services.AddScoped<IClientFinanceContactService, ClientFinanceContactService>();
            services.AddScoped<IClientAlertService, ClientAlertService>();
            services.AddScoped<IClientFinanceLearningService, ClientFinanceLearningService>();
            services.AddScoped<IClientFinanceParameterDetailService, ClientFinanceParameterDetailService>();
            services.AddScoped<IClientGlossaryService, ClientGlossaryService>();
            services.AddScoped<IClientFinanceService, ClientFinanceService>();
            services.AddScoped<IPortfolioService, PortfolioService>();
            services.AddScoped<IPortfolioAllocationService, PortfolioAllocationService>();
            services.AddScoped<ITechnicalIndicatorsService, TechnicalIndicatorsService>();
            services.AddScoped<IPortfolioRiskMetricsService, PortfolioRiskMetricsService>();
            services.AddScoped<ITaxService, TaxService>();
            services.AddScoped<ILearnTopicService, LearnTopicService>();
            services.AddScoped<IClientFinanceFundamentalsService, ClientFinanceFundamentalsService>();
            services.AddScoped<IScreenerService, ScreenerService>();
            return services;
        }

        internal static IServiceCollection AddAdminServices(this IServiceCollection services)
        {
            services.AddScoped<IFundamentalScoringService, FundamentalScoringService>();
            services.AddScoped<IAdminOverviewService, AdminOverviewService>();
            services.AddScoped<IAdminKpiService, AdminKpiService>();
            services.AddScoped<IAdminInstrumentRegistryService, AdminInstrumentRegistryService>();
            services.AddScoped<IAdminInstrumentSeedService, AdminInstrumentSeedService>();
            services.AddScoped<IAdminGlossaryService, AdminGlossaryService>();
            services.AddScoped<IAdminEducationService, AdminEducationService>();
            services.AddScoped<IAdminPeaRegistryService, AdminPeaRegistryService>();
            services.AddScoped<IAdminScoringPolicyService, AdminScoringPolicyService>();
            services.AddScoped<IAdminParameterDictionaryAdminService, AdminParameterDictionaryAdminService>();
            services.AddScoped<IAdminSnapshotAuditService, AdminSnapshotAuditService>();
            services.AddScoped<IAdminDataQualityService, AdminDataQualityService>();
            services.AddScoped<IWordingPublicationService, WordingPublicationService>();
            services.AddScoped<IAdminWordingVersionService, AdminWordingVersionService>();
            services.AddScoped<IAdminSignalQualityService, AdminSignalQualityService>();
            services.AddScoped<IEducationContentService, EducationContentService>();
            services.AddScoped<IGlossaryTermService, GlossaryTermService>();
            services.AddScoped<IFaqService, FaqService>();
            services.AddScoped<ILegalCardService, LegalCardService>();
            return services;
        }

        internal static IServiceCollection AddBackgroundJobs(this IServiceCollection services)
        {
            services.AddHostedService<AnalyticsRetentionJob>();
            services.AddHostedService<MarketDataRefreshJob>();
            services.AddHostedService<SignalOutcomeEvaluationJob>();
            services.AddHostedService<InstrumentWatchJob>();
            services.AddHostedService<AssetEnrichmentJob>();
            return services;
        }

        internal static IServiceCollection AddNotificationServices(this IServiceCollection services)
        {
            services.AddScoped<INotificationCenterService, NotificationCenterService>();
            services.AddScoped<IProactiveAlertEmitter, ProactiveAlertEmitter>();
            return services;
        }
    }
}
