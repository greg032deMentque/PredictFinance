namespace BackPredictFinance.ViewModels.ClientFinanceViewModels.Screener
{
    public sealed class ScreenerPresetViewModel
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public ScreenerQueryViewModel Query { get; set; } = new();
    }
}
