namespace BackPredictFinance.ViewModels.AdminViewModels.SignalQuality
{
    public sealed class ConfidenceCalibrationRowViewModel
    {
        public string Label { get; set; } = string.Empty;
        public int TotalSignals { get; set; }
        public int TargetHits { get; set; }
        public decimal HitRate { get; set; }
    }
}
