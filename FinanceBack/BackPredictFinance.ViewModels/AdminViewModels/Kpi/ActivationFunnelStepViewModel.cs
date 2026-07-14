namespace BackPredictFinance.ViewModels.AdminViewModels.Kpi
{
    public sealed class ActivationFunnelStepViewModel
    {
        public int Step { get; set; }
        public string Label { get; set; } = string.Empty;
        public int Count { get; set; }
        public decimal Rate { get; set; }
    }
}
