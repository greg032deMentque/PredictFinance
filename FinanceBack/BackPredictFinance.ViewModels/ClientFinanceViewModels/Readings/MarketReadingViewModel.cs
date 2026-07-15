namespace BackPredictFinance.ViewModels.ClientFinanceViewModels.Readings
{
    public sealed class MarketReadingViewModel : MarketReadingSummaryViewModel
    {
        public string ValidationSummary { get; set; } = string.Empty;
        public string PedagogicalSummary { get; set; } = string.Empty;
    }
}
