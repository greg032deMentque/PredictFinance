namespace BackPredictFinance.ViewModels.ClientFinanceViewModels.Patterns
{
    public sealed class PatternEvaluateResultViewModel
    {
        public string AnalysisId { get; set; } = string.Empty;
        public string Symbol { get; set; } = string.Empty;
        public List<PatternCandidateViewModel> Candidates { get; set; } = [];
    }
}
