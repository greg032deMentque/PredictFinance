namespace BackPredictFinance.ViewModels.AdminViewModels.Kpi
{
    public sealed class RetentionCohortViewModel
    {
        public string Label { get; set; } = string.Empty;
        public decimal Rate { get; set; }
        public int SampleSize { get; set; }
    }
}
