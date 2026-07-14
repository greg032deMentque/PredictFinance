using BackPredictFinance.Common.enums;

namespace BackPredictFinance.ViewModels.ClientFinanceViewModels.Analysis
{
    public sealed class RecommendationSummaryViewModel
    {
        public RecommendationKind Kind { get; set; } = RecommendationKind.Wait;
        public HoldingStatusEnum HoldingStatus { get; set; } = HoldingStatusEnum.NotHeld;
        public string DisplayLabel { get; set; } = string.Empty;
        public string ExplanationSummary { get; set; } = string.Empty;
        public string? WarningText { get; set; }
    }
}
