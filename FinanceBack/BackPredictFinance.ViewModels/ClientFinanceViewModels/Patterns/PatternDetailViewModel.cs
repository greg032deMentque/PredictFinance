using BackPredictFinance.ViewModels.ClientFinanceViewModels.Analysis;
using BackPredictFinance.ViewModels.ClientFinanceViewModels.Readings;

namespace BackPredictFinance.ViewModels.ClientFinanceViewModels.Patterns
{
    public sealed class PatternDetailViewModel
    {
        public string PatternId { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Phase { get; set; } = string.Empty;
        public ConfidenceBreakdownViewModel ConfidenceBreakdown { get; set; } = new();
        public decimal? NecklinePrice { get; set; }
        public decimal? TargetPrice { get; set; }
        public decimal? InvalidationPrice { get; set; }
        public string LifecyclePhaseCode { get; set; } = string.Empty;
        public string DetectionStatus { get; set; } = string.Empty;
        public string ValidationState { get; set; } = string.Empty;
        public string InvalidationState { get; set; } = string.Empty;
        public List<PatternScenarioBranchViewModel> ScenarioBranches { get; set; } = [];
        public RecommendationSummaryViewModel Posture { get; set; } = new();
    }
}
