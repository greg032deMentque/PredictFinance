using BackPredictFinance.ViewModels.ClientFinanceViewModels.Readings;

namespace BackPredictFinance.ViewModels.ClientFinanceViewModels.Instruments
{
    public sealed class InstrumentDetailViewModel
    {
        public string Symbol { get; set; } = string.Empty;
        public InstrumentSummaryViewModel InstrumentSummary { get; set; } = new();
        public MarketReadingViewModel MarketReading { get; set; } = new();
        public SupportReadingViewModel SupportReading { get; set; } = new();
        public PersonalSituationReadingViewModel PersonalSituation { get; set; } = new();
        public InstrumentNavigationLinksViewModel NavigationLinks { get; set; } = new();
        public string? LatestAnalysisId { get; set; }
        public string? LatestSnapshotId { get; set; }
    }
}
