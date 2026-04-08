namespace BackPredictFinance.Contracts.Analysis
{
    public sealed class AnalysisResponseTrace
    {
        public string TraceId { get; set; } = string.Empty;
        public string AnalysisEngineVersion { get; set; } = string.Empty;
        public string RuleSetVersion { get; set; } = string.Empty;
    }
}
