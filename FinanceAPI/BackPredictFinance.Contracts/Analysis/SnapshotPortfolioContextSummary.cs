namespace BackPredictFinance.Contracts.Analysis
{
    public sealed class SnapshotPortfolioContextSummary
    {
        public bool HoldsInstrument { get; set; }
        public decimal TotalQuantityHeld { get; set; }
        public decimal? AverageUnitCost { get; set; }
        public int OpenLineCount { get; set; }
        public string CurrencyCode { get; set; } = string.Empty;
    }
}
