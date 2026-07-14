namespace BackPredictFinance.ViewModels.AdminViewModels.SignalQuality
{
    public sealed class AdminSignalQualityKpiViewModel
    {
        public string Window { get; set; } = string.Empty;
        public string? PolicyVersionFilter { get; set; }
        public decimal OverallTargetHitRate { get; set; }
        public int TotalEvaluated { get; set; }
        public int OpenSignals { get; set; }
        public int NotEvaluable { get; set; }
        public List<ConfidenceCalibrationRowViewModel> ConfidenceCalibration { get; set; } = [];
        public List<PatternPerformanceRowViewModel> PatternPerformance { get; set; } = [];
        public List<ModelPerformanceRowViewModel> ModelPerformance { get; set; } = [];
    }

}