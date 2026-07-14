using BackPredictFinance.Services;
using BackPredictFinance.Services.AuthServices;
using BackPredictFinance.Services.ClientFinanceServices;
using BackPredictFinance.Patterns;
using BackPredictFinance.Patterns.Abstractions;
using BackPredictFinance.Patterns.Definitions;
using BackPredictFinance.Services.ClientFinanceServices.Alerts;
using BackPredictFinance.Services.ClientFinanceServices.Analysis;
using BackPredictFinance.Services.ClientFinanceServices.Patterns;
using BackPredictFinance.Services.TwelveDataServices;
using BackPredictFinance.Services.UserServices;
using BackPredictFinance.Services.Fundamentals;
using BackPredictFinance.Services.AdminGovernance;
using BackPredictFinance.Services.BackgroundJobs;
using BackPredictFinance.Services.Notifications;

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
            services.AddScoped<IUserPrivacyService, UserPrivacyService>();
            services.AddScoped<ICurrentUserSessionService, CurrentUserSessionService>();
            services.AddScoped<IUserAssetService, UserAssetService>();
            services.AddScoped<IUserRoleDataService, UserRoleDataService>();
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<IUserAdminService, UserAdminService>();
            
            services.AddScoped<AnalyticService>();
            services.AddHttpClient<YahooFinanceMarketDataProvider>();
            services.AddTransient<IMarketCatalogProvider>(serviceProvider => serviceProvider.GetRequiredService<YahooFinanceMarketDataProvider>());
            services.AddTransient<IMarketPriceProvider>(serviceProvider => serviceProvider.GetRequiredService<YahooFinanceMarketDataProvider>());
            services.AddTransient<IFundamentalsProvider>(serviceProvider => serviceProvider.GetRequiredService<YahooFinanceMarketDataProvider>());
            services.AddScoped<ITickerService, TickerService>();
            services.AddScoped<IPatternMarketDataProvider, PatternMarketDataProvider>();
            services.AddScoped<ITradingRecommendationService, TradingRecommendationService>();
            services.AddScoped<IAnalysisRequestCompatibilityResolver, AnalysisRequestCompatibilityResolver>();
            services.AddScoped<IAnalysisResultProjectionService, AnalysisResultProjectionService>();
            services.AddScoped<IAnalysisPatternDefinition, RectangleContinuationAnalysisPatternDefinition>();
            services.AddScoped<IAnalysisPatternDefinition, SymmetricalTriangleContinuationAnalysisPatternDefinition>();
            services.AddScoped<IAnalysisPatternDefinition, BullFlagContinuationAnalysisPatternDefinition>();
            services.AddScoped<IAnalysisPatternDefinition, BearFlagContinuationAnalysisPatternDefinition>();
            services.AddScoped<IAnalysisPatternRegistry, AnalysisPatternRegistry>();
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
            services.AddScoped<IClientFinanceAssetSupportService, ClientFinanceAssetSupportService>();
            services.AddScoped<IClientFinanceProjectionService, ClientFinanceProjectionService>();
            services.AddScoped<IClientFinanceWatchlistPortfolioService, ClientFinanceWatchlistPortfolioService>();
            services.AddScoped<IClientFinanceTransactionService, ClientFinanceTransactionService>();
            services.AddScoped<IClientFinanceDashboardHistoryService, ClientFinanceDashboardHistoryService>();
            services.AddScoped<IClientFinanceHistoryReadService, ClientFinanceHistoryReadService>();
            services.AddScoped<IClientFinanceInstrumentDetailService, ClientFinanceInstrumentDetailService>();
            services.AddScoped<IClientFinanceContactService, ClientFinanceContactService>();
            services.AddScoped<IClientFinanceSnapshotComparisonService, ClientFinanceSnapshotComparisonService>();
            services.AddScoped<IClientFinanceLearningService, ClientFinanceLearningService>();
            services.AddScoped<IClientFinanceParameterDetailService, ClientFinanceParameterDetailService>();
            services.AddScoped<IClientGlossaryService, ClientGlossaryService>();
            services.AddScoped<IEducationContentService, EducationContentService>();
            services.AddScoped<IGlossaryTermService, GlossaryTermService>();
            services.AddScoped<IFaqService, FaqService>();
            services.AddScoped<ILearnTopicService, LearnTopicService>();
            services.AddScoped<ILegalCardService, LegalCardService>();
            services.AddScoped<IPortfolioService, PortfolioService>();
            services.AddScoped<IClientAlertService, ClientAlertService>();
            services.AddScoped<IPatternExplorerService, PatternExplorerService>();
            services.AddScoped<IClientFinanceService, ClientFinanceService>();
            services.AddScoped<IFundamentalScoringService, FundamentalScoringService>();
            services.AddScoped<IAdminOverviewService, AdminOverviewService>();
            services.AddScoped<IAdminKpiService, AdminKpiService>();
            services.AddHostedService<AnalyticsRetentionJob>();
            services.AddScoped<IAdminInstrumentRegistryService, AdminInstrumentRegistryService>();
            services.AddScoped<IAdminInstrumentSeedService, AdminInstrumentSeedService>();
            services.AddScoped<IAdminEducationService, AdminEducationService>();
            services.AddScoped<IAdminGlossaryService, AdminGlossaryService>();
            services.AddScoped<IAdminPeaRegistryService, AdminPeaRegistryService>();
            services.AddScoped<IAdminScoringPolicyService, AdminScoringPolicyService>();
            services.AddScoped<IAdminParameterDictionaryAdminService, AdminParameterDictionaryAdminService>();
            services.AddScoped<IAdminSnapshotAuditService, AdminSnapshotAuditService>();
            services.AddScoped<IAdminDataQualityService, AdminDataQualityService>();
            services.AddScoped<IWordingPublicationService, WordingPublicationService>();
            services.AddScoped<IAdminWordingVersionService, AdminWordingVersionService>();
            services.AddScoped<INotificationCenterService, NotificationCenterService>();
            services.AddScoped<IProactiveAlertEmitter, ProactiveAlertEmitter>();
            services.AddScoped<IAdminSignalQualityService, AdminSignalQualityService>();
            services.AddHostedService<SignalOutcomeEvaluationJob>();
            services.AddHostedService<InstrumentWatchJob>();
        }
    }
}
