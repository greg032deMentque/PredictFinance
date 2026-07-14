using BackPredictFinance.Common.enums;
using BackPredictFinance.ViewModels.ClientFinanceViewModels.Analysis;

namespace BackPredictFinance.ViewModels.ClientFinanceViewModels.Readings
{
    public sealed class PersonalSituationReadingViewModel
    {
        public HoldingStatusEnum HoldingStatus { get; set; } = HoldingStatusEnum.NotHeld;
        public bool HoldsInstrument { get; set; }
        public decimal TotalQuantityHeld { get; set; }
        public decimal? AverageUnitCost { get; set; }
        public int? OpenLineCount { get; set; }
        public string CurrencyCode { get; set; } = string.Empty;
        public RecommendationSummaryViewModel Recommendation { get; set; } = new();
        public string GuidanceSummary { get; set; } = string.Empty;
    }
}
