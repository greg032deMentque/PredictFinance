namespace BackPredictFinance.ViewModels.ClientFinanceViewModels.Analysis
{
    public sealed class ExPostStatisticsViewModel
    {
        public string PatternId { get; set; } = string.Empty;
        public bool HasEarningsInWindow { get; set; }
        public int SampleSize { get; set; }
        public bool InsufficientData { get; set; }
        public decimal? WinRate { get; set; }
        public decimal? WinRateLow { get; set; }
        public decimal? WinRateHigh { get; set; }
        public bool SelectionBiasDisclaimer { get; set; } = true;
    }

    public sealed class ExPostPatternStatisticsViewModel
    {
        public List<ExPostStatisticsViewModel> PatternStats { get; set; } = [];
        public bool SelectionBiasDisclaimer { get; set; } = true;
    }
}
