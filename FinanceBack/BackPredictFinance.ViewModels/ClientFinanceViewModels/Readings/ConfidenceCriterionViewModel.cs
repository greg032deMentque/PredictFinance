namespace BackPredictFinance.ViewModels.ClientFinanceViewModels.Readings
{
    public sealed class ConfidenceCriterionViewModel
    {
        public string Code { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        // Token attendu par le front : met | partial | absent.
        public string State { get; set; } = string.Empty;
        // Token attendu par le front : DETECTION | VALIDATION | INVALIDATION.
        public string Source { get; set; } = string.Empty;
    }
}
