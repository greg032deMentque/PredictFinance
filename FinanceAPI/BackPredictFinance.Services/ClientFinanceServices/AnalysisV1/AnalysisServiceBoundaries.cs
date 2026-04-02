using BackPredictFinance.Common.enums;
using BackPredictFinance.Services.ClientFinanceServices;
using BackPredictFinance.ViewModels.ClientFinanceViewModels;
using BackPredictFinance.ViewModels.ClientFinanceViewModels.AnalysisV1;

namespace BackPredictFinance.Services.ClientFinanceServices.AnalysisV1
{
    public interface IAnalysisRequestCompatibilityResolver
    {
        Task<ResolvedAnalysisRunRequest> ResolveAsync(AnalysisRunRequestViewModel request, string userId, CancellationToken ct = default);
    }

    public interface IAnalysisLegacyCompatibilityService
    {
        AnalysisResultViewModel MapRunResult(AnalysisResponse response);
        Task<List<AnalysisResultViewModel>> GetRecentAnalysesAsync(string userId, int take, CancellationToken ct = default);
    }

    public interface IAnalysisOrchestrator
    {
        Task<AnalysisResponse> RunAnalysisAsync(ResolvedAnalysisRunRequest request, CancellationToken ct = default);
    }

    public interface IAnalysisPatternRegistry
    {
        ResolvedAnalysisPattern ResolveRequestedPattern(string? requestedPattern);
        IReadOnlyList<ResolvedAnalysisPattern> GetEnabledPatterns();
    }

    public interface IRecommendationPolicyService
    {
        Recommendation EvaluateAnalysis(ResolvedAnalysisRunRequest request, AnalysisExecutionArtifact executionArtifact);
    }

    public interface IAnalysisSnapshotPersistenceService
    {
        Task<PersistedAnalysisRecord> PersistSuccessfulAnalysisAsync(
            ResolvedAnalysisRunRequest request,
            ResolvedAnalysisPattern pattern,
            AnalysisExecutionArtifact executionArtifact,
            Recommendation recommendation,
            DateTime startedAtUtc,
            DateTime completedAtUtc,
            CancellationToken ct = default);

        Task PersistFailedAnalysisAsync(
            ResolvedAnalysisRunRequest request,
            ResolvedAnalysisPattern pattern,
            DateTime startedAtUtc,
            DateTime completedAtUtc,
            Exception exception,
            CancellationToken ct = default);
    }

    public interface IOptionalPythonAnalysisAdapter
    {
        Task<AnalysisExecutionArtifact> ExecuteAsync(ResolvedAnalysisRunRequest request, ResolvedAnalysisPattern pattern, CancellationToken ct = default);
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
        string BuildAnalysisSummary(AnalysisOutcome outcome, IReadOnlyList<PatternAssessment> compatiblePatterns, Recommendation? recommendation, PortfolioContext? portfolioContext);
    }

    public sealed class ResolvedAnalysisPattern
    {
        public string PatternId { get; set; } = string.Empty;
        public string ModelDir { get; set; } = string.Empty;
        public string ModelVersion { get; set; } = string.Empty;
    }

    public sealed class ResolvedAnalysisRunRequest
    {
        public string UserId { get; set; } = string.Empty;
        public string Symbol { get; set; } = string.Empty;
        public string RequestedPatternId { get; set; } = string.Empty;
        public bool HoldsInstrument { get; set; }
    }

    public sealed class ExecutedPatternArtifact
    {
        public TradingPatternEnum Pattern { get; set; } = TradingPatternEnum.DoubleTop;
        public string Phase { get; set; } = string.Empty;
        public decimal Probability { get; set; }
        public decimal Confidence { get; set; }
        public decimal CurrentPrice { get; set; }
        public decimal? NecklinePrice { get; set; }
        public decimal? TargetPrice { get; set; }
        public decimal? InvalidationPrice { get; set; }
        public DateTime? FirstPeakAtUtc { get; set; }
        public DateTime? SecondPeakAtUtc { get; set; }
        public bool IsPrimary { get; set; }
        public PatternAssessment ContractAssessment { get; set; } = new();
    }

    public sealed class AnalysisExecutionArtifact
    {
        public string Symbol { get; set; } = string.Empty;
        public DateTime GeneratedAtUtc { get; set; }
        public List<ExecutedPatternArtifact> Patterns { get; set; } = [];
        public ModelStatusEnum ModelStatus { get; set; } = ModelStatusEnum.NoGo;
        public string ModelMessage { get; set; } = string.Empty;
        public string ModelVersion { get; set; } = string.Empty;
        public decimal? Precision { get; set; }
        public decimal? F1 { get; set; }
        public decimal? RocAuc { get; set; }
        public int? PositiveSamples { get; set; }
        public decimal? SelectedThreshold { get; set; }
        public string RawProviderPayloadJson { get; set; } = string.Empty;
    }

    public sealed class PersistedAnalysisRecord
    {
        public string PublicId { get; set; } = string.Empty;
        public string InstrumentId { get; set; } = string.Empty;
        public string Symbol { get; set; } = string.Empty;
        public string ProviderSymbol { get; set; } = string.Empty;
        public string CompanyName { get; set; } = string.Empty;
        public string MarketCode { get; set; } = string.Empty;
        public string CurrencyCode { get; set; } = string.Empty;
        public string AssetType { get; set; } = string.Empty;
        public string CountryCode { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime? LastProfileSyncUtc { get; set; }
        public string? Summary { get; set; }
    }
}
