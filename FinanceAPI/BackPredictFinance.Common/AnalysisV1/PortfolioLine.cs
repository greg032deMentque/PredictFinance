namespace BackPredictFinance.Common.AnalysisV1
{
    public sealed class PortfolioLine
    {
        public string PortfolioLineId { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string InstrumentId { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public decimal UnitBuyPrice { get; set; }
        public DateOnly BuyDate { get; set; }
        public decimal FeesAmount { get; set; }
        public string CurrencyCode { get; set; } = string.Empty;
        public string? SourceReference { get; set; }
        public string? Note { get; set; }
    }
}
