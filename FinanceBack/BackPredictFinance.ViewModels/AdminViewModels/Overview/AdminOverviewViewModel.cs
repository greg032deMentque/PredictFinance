using BackPredictFinance.ViewModels.AdminViewModels.Kpi;

namespace BackPredictFinance.ViewModels.AdminViewModels.Overview
{
    public sealed class AdminOverviewViewModel
    {
        public int TotalUsers { get; set; }
        public int ActiveUsers { get; set; }
        public int TotalAssets { get; set; }
        public int TotalAnalysisRuns { get; set; }
        public int CompletedAnalysisRuns { get; set; }
        public int FailedAnalysisRuns { get; set; }
        public int ConfirmedEligiblePeaEntries { get; set; }
        public int UnknownPeaEntries { get; set; }
        public int PublishedParameterEntries { get; set; }
        public DateTime? LatestCompletedAnalysisUtc { get; set; }
        public List<AdminKpiCardViewModel> KpiCards { get; set; } = [];
    }
}
