namespace BackPredictFinance.ViewModels.ClientFinanceViewModels.Patterns
{
    public sealed class PatternCandidateViewModel
    {
        public string PatternId { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public decimal Confidence { get; set; }
        public decimal Probability { get; set; }
        public string ConfidenceLabel { get; set; } = string.Empty;
        public string Phase { get; set; } = string.Empty;
        public bool IsPrimary { get; set; }
        public decimal? NecklinePrice { get; set; }
        public decimal? TargetPrice { get; set; }
        public decimal? InvalidationPrice { get; set; }
    }
}
