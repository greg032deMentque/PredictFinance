using BackPredictFinance.Common.enums;
using BackPredictFinance.ViewModels.ClientFinanceViewModels.Analysis;
using BackPredictFinance.ViewModels.ClientFinanceViewModels.Assets;
using BackPredictFinance.ViewModels.ClientFinanceViewModels.Instruments;
using BackPredictFinance.ViewModels.ClientFinanceViewModels.Readings;

namespace BackPredictFinance.ViewModels.ClientFinanceViewModels.Dashboard
{
    public sealed class DashboardAttentionItemViewModel
    {
        public InstrumentIdentityViewModel Instrument { get; set; } = new();
        public HoldingStatusEnum HoldingStatus { get; set; } = HoldingStatusEnum.NotHeld;
        public MarketReadingSummaryViewModel MarketReading { get; set; } = new();
        public SupportReadingSummaryViewModel SupportReading { get; set; } = new();
        public RecommendationSummaryViewModel Recommendation { get; set; } = new();
        public FreshnessViewModel Freshness { get; set; } = new();
    }
}
