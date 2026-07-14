using BackPredictFinance.Common.enums;
using BackPredictFinance.ViewModels.ClientFinanceViewModels.Analysis;
using BackPredictFinance.ViewModels.ClientFinanceViewModels.Assets;
using BackPredictFinance.ViewModels.ClientFinanceViewModels.Instruments;
using BackPredictFinance.ViewModels.ClientFinanceViewModels.Readings;

namespace BackPredictFinance.ViewModels.ClientFinanceViewModels.Dashboard
{
    public sealed class DashboardRecentAnalysisItemViewModel
    {
        public string AnalysisId { get; set; } = string.Empty;
        public InstrumentIdentityViewModel Instrument { get; set; } = new();
        public DateTime TimestampUtc { get; set; }
        public TechnicalAnalysisOutcomeTypeEnum Outcome { get; set; } = TechnicalAnalysisOutcomeTypeEnum.NoCrediblePattern;
        public MarketReadingSummaryViewModel MarketReading { get; set; } = new();
        public SupportReadingSummaryViewModel SupportReading { get; set; } = new();
        public RecommendationSummaryViewModel Recommendation { get; set; } = new();
        public FreshnessViewModel Freshness { get; set; } = new();
    }
}
