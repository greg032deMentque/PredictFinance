namespace BackPredictFinance.Contracts.Analysis
{
    public sealed class ResolvedAnalysisPattern
    {
        public string PatternId { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string FamilyId { get; set; } = string.Empty;
        public string BiasCode { get; set; } = string.Empty;
        public string ModelDir { get; set; } = string.Empty;
        public string ModelVersion { get; set; } = string.Empty;
        public int HistoryLookbackMonths { get; set; }
        public int MinimumRequiredCandles { get; set; }
    }
}
