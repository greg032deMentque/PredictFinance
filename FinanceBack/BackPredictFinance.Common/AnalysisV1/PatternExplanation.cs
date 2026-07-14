namespace BackPredictFinance.Common.AnalysisV1
{
    public sealed class PatternExplanation
    {
        public string WhyListed { get; set; } = string.Empty;
        public string PedagogicalSummary { get; set; } = string.Empty;
        public string? AmbiguityNote { get; set; }
        public string? LimitationsNote { get; set; }
    }
}
