namespace BackPredictFinance.Contracts.Analysis
{
    public sealed class PatternTrace
    {
        public string PatternVersion { get; set; } = string.Empty;
        public string RuleSetVersion { get; set; } = string.Empty;
        public bool IsPrimaryDisplayCandidate { get; set; }
        public string? ScoringVersion { get; set; }
    }
}
