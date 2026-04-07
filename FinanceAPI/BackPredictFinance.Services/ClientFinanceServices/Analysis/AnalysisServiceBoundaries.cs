using BackPredictFinance.Common.enums;
using BackPredictFinance.Services.ClientFinanceServices;
using BackPredictFinance.ViewModels.ClientFinanceViewModels;
using BackPredictFinance.ViewModels.ClientFinanceViewModels.AnalysisV1;
using BackPredictFinance.Contracts.Analysis;

namespace BackPredictFinance.Services.ClientFinanceServices.Analysis
{
    public interface IAnalysisRequestCompatibilityResolver
    {
        Task<AnalysisRequest> ResolveAsync(AnalysisRunRequestViewModel request, string userId, CancellationToken ct = default);
    }

    public interface IAnalysisLegacyCompatibilityService
    {
        AnalysisResultViewModel MapRunResult(AnalysisResponse response);
        Task<List<AnalysisResultViewModel>> GetRecentAnalysesAsync(string userId, int take, CancellationToken ct = default);
    }

    public interface IAnalysisOrchestrator
    {
        Task<AnalysisResponse> RunAnalysisAsync(AnalysisRequest request, CancellationToken ct = default);
    }

    public interface IAnalysisExecutionService
    {
        Task<AnalysisExecutionArtifact> ExecuteAsync(AnalysisRequest request, ResolvedAnalysisPattern pattern, CancellationToken ct = default);
    }

    public interface IAnalysisPatternDefinition
    {
        string PatternId { get; }
        string ModelVersion { get; }
        int HistoryLookbackMonths { get; }
        ResolvedAnalysisPattern BuildResolvedPattern();
        Task<AnalysisExecutionArtifact> ExecuteAsync(AnalysisRequest request, CancellationToken ct = default);
    }

    public interface IAnalysisPatternRegistry
    {
        ResolvedAnalysisPattern ResolveRequestedPattern(string? requestedPattern);
        IAnalysisPatternDefinition ResolveDefinition(string? requestedPattern);
        IReadOnlyList<ResolvedAnalysisPattern> GetEnabledPatterns();
    }

    public interface IRecommendationPolicyService
    {
        Recommendation EvaluateAnalysis(AnalysisRequest request, IReadOnlyList<PatternAssessment> compatiblePatterns, AnalysisOutcome outcome);
    }

    public interface IAnalysisSnapshotPersistenceService
    {
        Task<PersistedAnalysisRecord> PersistSuccessfulAnalysisAsync(
            AnalysisRequest request,
            ResolvedAnalysisPattern pattern,
            AnalysisExecutionArtifact executionArtifact,
            Recommendation recommendation,
            AnalysisOutcome outcome,
            string pedagogicalSummary,
            string explanationPolicyVersion,
            DateTime startedAtUtc,
            DateTime completedAtUtc,
            CancellationToken ct = default);

        Task PersistFailedAnalysisAsync(
            AnalysisRequest request,
            ResolvedAnalysisPattern pattern,
            DateTime startedAtUtc,
            DateTime completedAtUtc,
            Exception exception,
            CancellationToken ct = default);
    }

    public interface IPortfolioContextLoader
    {
        Task<PortfolioContext?> TryLoadAsync(string userId, string instrumentId, DateOnly? asOfDate, CancellationToken ct = default);
    }

    public interface IRiskEvaluationService
    {
        PatternRiskHints EvaluatePrimaryRisk(AnalysisExecutionArtifact executionArtifact, PatternAssessment patternAssessment);
    }

    public interface IPedagogicalExplanationService
    {
        string PolicyVersion { get; }
        PatternExplanation BuildPatternExplanation(PatternAssessment patternAssessment, bool hasMultipleCompatiblePatterns, bool hasModelWarning);
        string BuildAnalysisSummary(AnalysisOutcome outcome, IReadOnlyList<PatternAssessment> compatiblePatterns, Recommendation? recommendation, PortfolioContext? portfolioContext);
    }

}
