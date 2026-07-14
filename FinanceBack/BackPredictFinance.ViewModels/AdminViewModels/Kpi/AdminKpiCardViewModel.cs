namespace BackPredictFinance.ViewModels.AdminViewModels.Kpi
{
    public sealed class AdminKpiCardViewModel
    {
        public string Key { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public decimal Value { get; set; }
        public decimal? PreviousValue { get; set; }
        public string Unit { get; set; } = string.Empty;
    }
}
