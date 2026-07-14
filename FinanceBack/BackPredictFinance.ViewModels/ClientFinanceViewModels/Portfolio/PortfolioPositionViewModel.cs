using BackPredictFinance.ViewModels.ClientFinanceViewModels.Analysis;
using BackPredictFinance.ViewModels.ClientFinanceViewModels.Instruments;
using BackPredictFinance.ViewModels.ClientFinanceViewModels.Readings;

namespace BackPredictFinance.ViewModels.ClientFinanceViewModels.Portfolio
{
    public sealed class PortfolioPositionViewModel
    {
        public string UserAssetId { get; set; } = string.Empty;
        public InstrumentIdentityViewModel Instrument { get; set; } = new();
        public decimal QuantityHeld { get; set; }
        public decimal AverageCost { get; set; }
        public decimal Fees { get; set; }
        public decimal OutstandingAmount { get; set; }
        public MarketReadingSummaryViewModel MarketReading { get; set; } = new();
        public SupportReadingSummaryViewModel SupportReading { get; set; } = new();
        public RecommendationSummaryViewModel Recommendation { get; set; } = new();
        public string? RiskHint { get; set; }
        public string? HistoryEntryUrl { get; set; }
        public string? SimulationUrl { get; set; }
    }
}
