namespace BackPredictFinance.Contracts.Analysis
{
    public sealed class PatternScoring
    {
        public decimal ConfidenceScore { get; set; }
        public string ConfidenceLabel { get; set; } = string.Empty;
        public bool IsCredible { get; set; }
        public List<string> ScoreReasons { get; set; } = [];
        public string? ScoreVersion { get; set; }
    }
}
