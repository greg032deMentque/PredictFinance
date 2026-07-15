namespace BackPredictFinance.ViewModels.ClientFinanceViewModels.Fundamentals
{
    public sealed class InstrumentFundamentalsViewModel
    {
        public string Symbol { get; set; } = string.Empty;
        public string CompanyName { get; set; } = string.Empty;
        public string Sector { get; set; } = string.Empty;
        public string Currency { get; set; } = string.Empty;
        public DateTime AsOfUtc { get; set; }
        public decimal? TrailingPe { get; set; }
        public decimal? DividendYield { get; set; }
        public decimal? ReturnOnEquity { get; set; }
        public decimal? OperatingMargin { get; set; }
        public decimal? CurrentRatio { get; set; }
        public decimal? DebtToEquity { get; set; }
        public decimal? RevenueGrowth { get; set; }
        public decimal? EarningsGrowth { get; set; }
        public decimal? PegRatio { get; set; }
        public decimal? PriceToBook { get; set; }
        public string? RecommendationKey { get; set; }
        public decimal? RecommendationMean { get; set; }
        public decimal? TargetMeanPrice { get; set; }
    }
}
