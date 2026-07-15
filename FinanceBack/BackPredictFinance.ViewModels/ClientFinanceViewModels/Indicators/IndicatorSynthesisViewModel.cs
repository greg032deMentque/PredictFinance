namespace BackPredictFinance.ViewModels.ClientFinanceViewModels.Indicators
{
    public class IndicatorSynthesisViewModel
    {
        public string Label { get; set; } = string.Empty;
        public int BullishSignals { get; set; }
        public int BearishSignals { get; set; }
        public int TotalSignals { get; set; }
    }
}
