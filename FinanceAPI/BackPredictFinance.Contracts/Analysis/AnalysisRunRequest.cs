namespace BackPredictFinance.Contracts.Analysis
{
    public sealed class AnalysisRunRequest
    {
        public string Symbol { get; set; } = string.Empty;
        public string RequestedPattern { get; set; } = string.Empty;
    }
}
