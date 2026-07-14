using BackPredictFinance.Common.enums;
using BackPredictFinance.ViewModels.ClientFinanceViewModels.Instruments;

namespace BackPredictFinance.ViewModels.ClientFinanceViewModels.History
{
    public sealed class HistoryItemViewModel
    {
        public string AnalysisId { get; set; } = string.Empty;
        public string SnapshotId { get; set; } = string.Empty;
        public InstrumentIdentityViewModel Instrument { get; set; } = new();
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
        public string HistoryUrl { get; set; } = string.Empty;
        public string ComparisonUrl { get; set; } = string.Empty;
    }
}
