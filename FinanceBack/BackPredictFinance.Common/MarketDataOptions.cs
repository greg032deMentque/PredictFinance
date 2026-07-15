namespace BackPredictFinance.Common
{
    public sealed class MarketDataOptions
    {
        public string YahooSessionBootstrapUrl { get; set; } = "https://fc.yahoo.com/";
        public string YahooCrumbUrl { get; set; } = "https://query1.finance.yahoo.com/v1/test/getcrumb";
        public string YahooSearchUrl { get; set; } = "https://query1.finance.yahoo.com/v1/finance/search";
        public string YahooChartUrl { get; set; } = "https://query1.finance.yahoo.com/v8/finance/chart";
        public string YahooQuoteSummaryUrl { get; set; } = "https://query1.finance.yahoo.com/v10/finance/quoteSummary";
        public int SearchCacheMinutes { get; set; } = 10;
        public int QuoteCacheMinutes { get; set; } = 5;
        public int ProfileCacheMinutes { get; set; } = 60;
        public int ChartCacheMinutes { get; set; } = 30;
        public string UserAgent { get; set; } = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/137.0.0.0 Safari/537.36";
        public int ResilienceMaxRetryAttempts { get; set; } = 3;
        public int ResilienceBaseDelaySeconds { get; set; } = 2;
        public double CircuitBreakerFailureRatio { get; set; } = 0.5;
        public int CircuitBreakerSamplingSeconds { get; set; } = 30;
        public int CircuitBreakerMinimumThroughput { get; set; } = 5;
        public int HttpAttemptTimeoutSeconds { get; set; } = 15;
        public int RefreshScheduledHourUtc { get; set; } = 19;
        public int RefreshThrottleMilliseconds { get; set; } = 500;
        public int DegradedModeMaxSnapshotAgeHours { get; set; } = 48;
    }
}
