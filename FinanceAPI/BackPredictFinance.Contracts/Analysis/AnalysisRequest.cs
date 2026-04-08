namespace BackPredictFinance.Contracts.Analysis
{
    public sealed class AnalysisRequest
    {
        public string InstrumentId { get; set; } = string.Empty;
        public List<string> RequestedPatternIds { get; set; } = [];
        public DateOnly? AsOfDate { get; set; }
        public string UserId { get; set; } = string.Empty;
        public Instrument Instrument { get; set; } = new();
        public PortfolioContext PortfolioContext { get; set; } = new();
        public string CandleInterval { get; set; } = "1d";
        public string AnalysisMode { get; set; } = "on_demand";
        public List<string> ResolvedPatternIds { get; set; } = [];
        public DateOnly HistoryStartDate { get; set; }
        public DateOnly HistoryEndDate { get; set; }
    }
}
