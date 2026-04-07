namespace BackPredictFinance.Common.AnalysisV1
{
    public sealed class PatternInvalidation
    {
        public string State { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
        public decimal? InvalidationLevel { get; set; }
        public DateOnly? BreachedAtDate { get; set; }
        public decimal? BreachedAtPrice { get; set; }
        public string? InvalidationRuleCode { get; set; }
    }
}
