namespace BackPredictFinance.ViewModels.AdminViewModels.SignalQuality
{
    public sealed class ModelPerformanceRowViewModel
    {
        public string ModelVersion { get; set; } = string.Empty;
        public int TotalEvaluated { get; set; }
        public decimal TargetHitRate { get; set; }
    }

}