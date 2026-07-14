namespace BackPredictFinance.Common.MarketData
{
    public sealed class MarketEtfProfileData
    {
        public string Symbol { get; set; } = string.Empty;
        public string? FundFamily { get; set; }
        public string? Category { get; set; }
        public string? LegalType { get; set; }
        public string? IndexTracked { get; set; }
        public decimal? TotalExpenseRatio { get; set; }
        public decimal? TotalAssets { get; set; }
        public string? ReplicationMethod { get; set; }
        public decimal? YtdReturn { get; set; }
        public decimal? ThreeYearAverageReturn { get; set; }
        public decimal? FiveYearAverageReturn { get; set; }
    }
}
