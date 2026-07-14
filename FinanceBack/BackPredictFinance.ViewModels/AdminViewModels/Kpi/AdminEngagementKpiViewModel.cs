namespace BackPredictFinance.ViewModels.AdminViewModels.Kpi
{
    public sealed class AdminEngagementKpiViewModel
    {
        public string Window { get; set; } = string.Empty;
        public int Dau { get; set; }
        public int Wau { get; set; }
        public int Mau { get; set; }
        public decimal Stickiness { get; set; }
        public int ActiveUsers { get; set; }
        public List<RetentionCohortViewModel> RetentionCohorts { get; set; } = [];
        public List<ActivationFunnelStepViewModel> ActivationFunnel { get; set; } = [];
        public decimal NotificationReadRate { get; set; }
        public decimal OpsSuccessRate { get; set; }
        public double OpsAvgDurationMs { get; set; }
        public int StaleAssets { get; set; }
    }
}
