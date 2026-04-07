using BackPredictFinance.Common.enums;

namespace BackPredictFinance.Common.AnalysisV1
{
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
}
