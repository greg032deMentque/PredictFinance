namespace BackPredictFinance.Contracts.Analysis
{
    public sealed class PatternValidation
    {
        public string State { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
        public DateOnly? ValidatedAtDate { get; set; }
        public decimal? ValidatedAtPrice { get; set; }
        public string? ValidationRuleCode { get; set; }
    }
}
