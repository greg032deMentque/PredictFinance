namespace BackPredictFinance.Patterns.Contracts
{
    public sealed class ResolvedAnalysisPattern
    {
        public string PatternId { get; set; } = string.Empty;
        public string ModelDir { get; set; } = string.Empty;
        public string ModelVersion { get; set; } = string.Empty;
        public int HistoryLookbackMonths { get; set; }
    }
}
