namespace BackPredictFinance.Contracts.Analysis
{
    public sealed class PortfolioContextLine
    {
        public decimal Quantity { get; set; }
        public decimal UnitBuyPrice { get; set; }
        public DateOnly BuyDate { get; set; }
        public decimal FeesAmount { get; set; }
        public string CurrencyCode { get; set; } = string.Empty;
    }
}
