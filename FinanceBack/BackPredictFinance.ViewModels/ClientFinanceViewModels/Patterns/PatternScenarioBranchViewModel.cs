namespace BackPredictFinance.ViewModels.ClientFinanceViewModels.Patterns
{
    public sealed class PatternScenarioBranchViewModel
    {
        public string TriggerLabel { get; set; } = string.Empty;
        public decimal? TriggerLevel { get; set; }
        public string Direction { get; set; } = string.Empty;
        public string ResultingState { get; set; } = string.Empty;
        public string Posture { get; set; } = string.Empty;
        public string Rationale { get; set; } = string.Empty;
    }
}
