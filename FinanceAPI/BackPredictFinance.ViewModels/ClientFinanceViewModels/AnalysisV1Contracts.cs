namespace BackPredictFinance.ViewModels.ClientFinanceViewModels.AnalysisV1
{
    public enum RecommendationKind
    {
        Monitor,
        Buy,
        Wait,
        Hold,
        Reinforce,
        Lighten,
        Sell
    }

    public enum PatternStatus
    {
        Forming,
        Monitoring,
        Confirmed,
        Invalidated,
        Completed
    }

    public enum AnalysisOutcome
    {
        CrediblePatternFound,
        MultipleCompatiblePatterns,
        NoCrediblePattern,
        InsufficientData,
        UnsupportedInstrument,
        UnsupportedContext
    }

    public sealed class Instrument
    {
        public string InstrumentId { get; set; } = string.Empty;
        public string Symbol { get; set; } = string.Empty;
        public string ProviderSymbol { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string MarketCode { get; set; } = string.Empty;
        public string CountryCode { get; set; } = string.Empty;
        public string CurrencyCode { get; set; } = string.Empty;
        public string AssetType { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime? LastProfileSyncUtc { get; set; }
        public string? Summary { get; set; }
    }

    public sealed class PortfolioLine
    {
        public string PortfolioLineId { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string InstrumentId { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public decimal UnitBuyPrice { get; set; }
        public DateOnly BuyDate { get; set; }
        public decimal FeesAmount { get; set; }
        public string CurrencyCode { get; set; } = string.Empty;
        public string? SourceReference { get; set; }
        public string? Note { get; set; }
    }

    public sealed class PortfolioContextLine
    {
        public decimal Quantity { get; set; }
        public decimal UnitBuyPrice { get; set; }
        public DateOnly BuyDate { get; set; }
        public decimal FeesAmount { get; set; }
        public string CurrencyCode { get; set; } = string.Empty;
    }

    public sealed class PortfolioContext
    {
        public string UserId { get; set; } = string.Empty;
        public string InstrumentId { get; set; } = string.Empty;
        public bool HoldsInstrument { get; set; }
        public int OpenLineCount { get; set; }
        public decimal TotalQuantityHeld { get; set; }
        public decimal? AverageUnitCost { get; set; }
        public string CurrencyCode { get; set; } = string.Empty;
        public List<PortfolioContextLine> OpenLines { get; set; } = [];
        public DateOnly? OldestOpenBuyDate { get; set; }
        public DateOnly? LatestOpenBuyDate { get; set; }
    }

    public sealed class AnalysisRequest
    {
        public string InstrumentId { get; set; } = string.Empty;
        public List<string> RequestedPatternIds { get; set; } = [];
        public DateOnly? AsOfDate { get; set; }
        public string UserId { get; set; } = string.Empty;
        public Instrument Instrument { get; set; } = new();
        public PortfolioContext PortfolioContext { get; set; } = new();
        public string CandleInterval { get; set; } = "1d";
        public string AnalysisMode { get; set; } = "on_demand";
        public List<string> ResolvedPatternIds { get; set; } = [];
        public DateOnly HistoryStartDate { get; set; }
        public DateOnly HistoryEndDate { get; set; }
    }

    public sealed class PatternAnalysisWindow
    {
        public string Interval { get; set; } = "1d";
        public DateOnly StartDate { get; set; }
        public DateOnly EndDate { get; set; }
        public int RequiredCandles { get; set; }
        public int ActualCandles { get; set; }
    }

    public sealed class PatternStructuralPoint
    {
        public string PointType { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public decimal Price { get; set; }
    }

    public sealed class PatternDetection
    {
        public bool IsCompatible { get; set; }
        public PatternStatus Status { get; set; }
        public string CurrentPhaseCode { get; set; } = string.Empty;
        public string CurrentPhaseLabel { get; set; } = string.Empty;
        public string StatusReason { get; set; } = string.Empty;
        public decimal CurrentPrice { get; set; }
        public List<PatternStructuralPoint> StructuralPoints { get; set; } = [];
    }

    public sealed class PatternValidation
    {
        public string State { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
        public DateOnly? ValidatedAtDate { get; set; }
        public decimal? ValidatedAtPrice { get; set; }
        public string? ValidationRuleCode { get; set; }
    }

    public sealed class PatternInvalidation
    {
        public string State { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
        public decimal? InvalidationLevel { get; set; }
        public DateOnly? BreachedAtDate { get; set; }
        public decimal? BreachedAtPrice { get; set; }
        public string? InvalidationRuleCode { get; set; }
    }

    public sealed class PatternScoring
    {
        public decimal ConfidenceScore { get; set; }
        public string ConfidenceLabel { get; set; } = string.Empty;
        public bool IsCredible { get; set; }
        public List<string> ScoreReasons { get; set; } = [];
        public string? ScoreVersion { get; set; }
    }

    public sealed class PatternRiskHints
    {
        public bool HasRiskPlan { get; set; }
        public decimal? SuggestedStopLoss { get; set; }
        public decimal? SuggestedTakeProfit { get; set; }
        public decimal? RiskRewardRatio { get; set; }
        public string? PositioningNote { get; set; }
    }

    public sealed class PatternExplanation
    {
        public string WhyListed { get; set; } = string.Empty;
        public string PedagogicalSummary { get; set; } = string.Empty;
        public string? AmbiguityNote { get; set; }
        public string? LimitationsNote { get; set; }
    }

    public sealed class PatternTrace
    {
        public string PatternVersion { get; set; } = string.Empty;
        public string RuleSetVersion { get; set; } = string.Empty;
        public bool IsPrimaryDisplayCandidate { get; set; }
        public string? ScoringVersion { get; set; }
    }

    public sealed class PatternAssessment
    {
        public string AssessmentId { get; set; } = string.Empty;
        public string PatternId { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string PedagogicalDescription { get; set; } = string.Empty;
        public PatternAnalysisWindow AnalysisWindow { get; set; } = new();
        public PatternDetection Detection { get; set; } = new();
        public PatternValidation Validation { get; set; } = new();
        public PatternInvalidation Invalidation { get; set; } = new();
        public PatternScoring Scoring { get; set; } = new();
        public PatternRiskHints RiskHints { get; set; } = new();
        public PatternExplanation Explanation { get; set; } = new();
        public PatternTrace Trace { get; set; } = new();
    }

    public sealed class Recommendation
    {
        public string RecommendationId { get; set; } = string.Empty;
        public RecommendationKind Kind { get; set; }
        public string HoldingContext { get; set; } = string.Empty;
        public string Rationale { get; set; } = string.Empty;
        public List<string> BasedOnPatternIds { get; set; } = [];
        public int? ReviewHorizonDays { get; set; }
        public string PolicyVersion { get; set; } = string.Empty;
        public string? WarningText { get; set; }
    }

    public sealed class SnapshotPortfolioContextSummary
    {
        public bool HoldsInstrument { get; set; }
        public decimal TotalQuantityHeld { get; set; }
        public decimal? AverageUnitCost { get; set; }
        public int OpenLineCount { get; set; }
        public string CurrencyCode { get; set; } = string.Empty;
    }

    public sealed class AnalysisSnapshotPatternRow
    {
        public string SnapshotPatternRowId { get; set; } = string.Empty;
        public string SnapshotId { get; set; } = string.Empty;
        public string PatternId { get; set; } = string.Empty;
        public int DisplayRank { get; set; }
        public bool IsCompatible { get; set; }
        public bool IsPrimaryDisplayCandidate { get; set; }
        public PatternAssessment PatternAssessmentPayload { get; set; } = new();
    }

    public sealed class AnalysisSnapshotRecommendation
    {
        public string SnapshotRecommendationId { get; set; } = string.Empty;
        public string SnapshotId { get; set; } = string.Empty;
        public Recommendation RecommendationPayload { get; set; } = new();
        public DateTime CreatedAtUtc { get; set; }
    }

    public sealed class AnalysisSnapshot
    {
        public string SnapshotId { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string InstrumentId { get; set; } = string.Empty;
        public List<string> RequestedPatternIds { get; set; } = [];
        public List<string> ExecutedPatternIds { get; set; } = [];
        public AnalysisOutcome Outcome { get; set; }
        public DateTime RequestedAtUtc { get; set; }
        public DateTime CompletedAtUtc { get; set; }
        public DateOnly AsOfDate { get; set; }
        public string CandleInterval { get; set; } = "1d";
        public string MarketDataProviderCode { get; set; } = string.Empty;
        public DateOnly MarketDataRangeStart { get; set; }
        public DateOnly MarketDataRangeEnd { get; set; }
        public SnapshotPortfolioContextSummary PortfolioContextSnapshot { get; set; } = new();
        public string? PrimaryPatternId { get; set; }
        public string? RecommendationId { get; set; }
        public string TraceId { get; set; } = string.Empty;
        public string? ErrorCode { get; set; }
        public string? ErrorMessage { get; set; }
        public string AnalysisEngineVersion { get; set; } = string.Empty;
        public string MarketNormalizationVersion { get; set; } = string.Empty;
        public string RecommendationPolicyVersion { get; set; } = string.Empty;
        public string ExplanationPolicyVersion { get; set; } = string.Empty;
        public List<AnalysisSnapshotPatternRow> PatternRows { get; set; } = [];
        public AnalysisSnapshotRecommendation? Recommendation { get; set; }
    }

    public sealed class AnalysisResponseTrace
    {
        public string TraceId { get; set; } = string.Empty;
        public string AnalysisEngineVersion { get; set; } = string.Empty;
        public string RuleSetVersion { get; set; } = string.Empty;
    }

    public sealed class AnalysisResponse
    {
        public string AnalysisId { get; set; } = string.Empty;
        public DateTime GeneratedAtUtc { get; set; }
        public DateOnly AsOfDate { get; set; }
        public AnalysisOutcome Outcome { get; set; }
        public Instrument Instrument { get; set; } = new();
        public List<string> RequestedPatternIds { get; set; } = [];
        public List<string> ExecutedPatternIds { get; set; } = [];
        public PatternAssessment? MainPattern { get; set; }
        public List<PatternAssessment> AlternativePatterns { get; set; } = [];
        public Recommendation? Recommendation { get; set; }
        public string PedagogicalSummary { get; set; } = string.Empty;
        public string? NoCrediblePatternReason { get; set; }
        public AnalysisResponseTrace Trace { get; set; } = new();
        public List<string> Warnings { get; set; } = [];
    }
}
