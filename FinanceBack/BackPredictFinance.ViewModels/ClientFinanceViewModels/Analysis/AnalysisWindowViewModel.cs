namespace BackPredictFinance.ViewModels.ClientFinanceViewModels.Analysis
{
    public sealed class AnalysisWindowViewModel
    {
        public string Interval { get; set; } = string.Empty;
        public DateOnly StartDate { get; set; }
        public DateOnly EndDate { get; set; }
        public int RequiredCandles { get; set; }
        public int ActualCandles { get; set; }
    }
}
