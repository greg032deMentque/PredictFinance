namespace BackPredictFinance.Common
{
    public sealed class MarketDataOptions
    {
        public string YahooSearchUrl { get; set; } = "https://query1.finance.yahoo.com/v1/finance/search";
        public string YahooChartUrl { get; set; } = "https://query1.finance.yahoo.com/v8/finance/chart";
        public string YahooQuoteSummaryUrl { get; set; } = "https://query1.finance.yahoo.com/v10/finance/quoteSummary";
        public int SearchCacheMinutes { get; set; } = 10;
        public int QuoteCacheMinutes { get; set; } = 5;
        public int ProfileCacheMinutes { get; set; } = 60;
        public int ChartCacheMinutes { get; set; } = 30;
        public string UserAgent { get; set; } = "PredictFinance/1.0";
    }
}
