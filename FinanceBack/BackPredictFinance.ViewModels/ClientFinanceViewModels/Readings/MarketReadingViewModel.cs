using BackPredictFinance.Common.enums;
using BackPredictFinance.ViewModels.ClientFinanceViewModels.Analysis;

namespace BackPredictFinance.ViewModels.ClientFinanceViewModels.Readings
{
    public sealed class MarketReadingViewModel
    {
        public TechnicalAnalysisOutcomeTypeEnum Outcome { get; set; } = TechnicalAnalysisOutcomeTypeEnum.NoCrediblePattern;
        public string OutcomeDisplayLabel { get; set; } = string.Empty;
        public string? PrimaryPatternId { get; set; }
        public string? PrimaryPatternDisplayName { get; set; }
        public PatternProgressStatusEnum ProgressStatus { get; set; } = PatternProgressStatusEnum.Absent;
        public string? ConfidenceLabel { get; set; }
        public RecommendationStrengthEnum? RecommendationStrength { get; set; }
        public ValidationStateEnum ValidationState { get; set; } = ValidationStateEnum.NotApplicable;
        public string ValidationSummary { get; set; } = string.Empty;
        public decimal? InvalidationLevel { get; set; }
        public string? RiskHint { get; set; }
        public string PedagogicalSummary { get; set; } = string.Empty;
        public List<AlternativePatternViewModel> Alternatives { get; set; } = [];
    }
}
