namespace BackPredictFinance.Contracts.Analysis
{
    public sealed class PatternAnalysisWindow
    {
        public string Interval { get; set; } = "1d";
        public DateOnly StartDate { get; set; }
        public DateOnly EndDate { get; set; }
        public int RequiredCandles { get; set; }
        public int ActualCandles { get; set; }
    }
}
