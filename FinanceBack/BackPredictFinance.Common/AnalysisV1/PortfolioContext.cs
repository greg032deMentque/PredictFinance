namespace BackPredictFinance.Common.AnalysisV1
{
    public sealed class PortfolioContext
    {
        public string UserId { get; set; } = string.Empty;
        public string InstrumentId { get; set; } = string.Empty;
        public bool HoldsInstrument { get; set; }
        public int OpenLineCount { get; set; }
        public decimal TotalQuantityHeld { get; set; }
        public decimal? AverageUnitCost { get; set; }
        public string CurrencyCode { get; set; } = string.Empty;
        public List<PortfolioContextLine> OpenLines { get; set; } = [];
        public DateOnly? OldestOpenBuyDate { get; set; }
        public DateOnly? LatestOpenBuyDate { get; set; }
        public bool HasDataIntegrityWarning { get; set; }
    }
}
