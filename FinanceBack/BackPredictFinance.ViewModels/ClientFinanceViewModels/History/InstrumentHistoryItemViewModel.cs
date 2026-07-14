using BackPredictFinance.Common.enums;

namespace BackPredictFinance.ViewModels.ClientFinanceViewModels.History
{
    public sealed class InstrumentHistoryItemViewModel
    {
        public string AnalysisId { get; set; } = string.Empty;
        public string SnapshotId { get; set; } = string.Empty;
        public DateTime TimestampUtc { get; set; }
        public TechnicalAnalysisOutcomeTypeEnum Outcome { get; set; } = TechnicalAnalysisOutcomeTypeEnum.NoCrediblePattern;
        public string OutcomeDisplayLabel { get; set; } = string.Empty;
        public string? PrimaryPatternLabel { get; set; }
        public string RecommendationSummary { get; set; } = string.Empty;
        public string SupportAvailabilitySummary { get; set; } = string.Empty;
        public PeaEligibilityStatusEnum PeaEligibilityStatus { get; set; } = PeaEligibilityStatusEnum.Unknown;
        public string PeaSummary { get; set; } = string.Empty;
        public string AnalysisEngineVersion { get; set; } = string.Empty;
        public string? RecommendationPolicyVersion { get; set; }
        public string? ExplanationPolicyVersion { get; set; }
        public string DetailUrl { get; set; } = string.Empty;
        public string ComparisonUrl { get; set; } = string.Empty;
    }
}
