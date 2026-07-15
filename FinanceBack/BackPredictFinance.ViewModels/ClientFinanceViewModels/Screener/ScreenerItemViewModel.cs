namespace BackPredictFinance.ViewModels.ClientFinanceViewModels.Screener
{
    public sealed class ScreenerItemViewModel
    {
        public string Symbol { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Exchange { get; set; } = string.Empty;
        public string? Country { get; set; }
        public string? Sector { get; set; }
        public int AssetType { get; set; }
        public bool IsPeaEligible { get; set; }
        public decimal? LastPrice { get; set; }
        public decimal? DayVariationPct { get; set; }
        public DateTime? QuoteAsOfUtc { get; set; }
        public decimal? TrailingPE { get; set; }
        public decimal? DividendYield { get; set; }
        public decimal? MarketCap { get; set; }
    }
}
