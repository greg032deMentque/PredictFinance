namespace BackPredictFinance.ViewModels.ClientFinanceViewModels.Indicators
{
    public class MacdIndicatorViewModel
    {
        public decimal Line { get; set; }
        public decimal SignalLine { get; set; }
        public decimal Histogram { get; set; }
        public string Trend { get; set; } = string.Empty;
    }
}
