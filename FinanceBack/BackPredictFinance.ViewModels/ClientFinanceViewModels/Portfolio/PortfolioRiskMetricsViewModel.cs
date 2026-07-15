namespace BackPredictFinance.ViewModels.ClientFinanceViewModels.Portfolio
{
    public sealed class PortfolioRiskMetricsViewModel
    {
        public int DataPointsUsed { get; set; }
        public decimal? Twr { get; set; }
        public decimal? SharpeRatio { get; set; }
        public decimal? AnnualizedVolatility { get; set; }
        public decimal? MaxDrawdown { get; set; }
        public DateTime? PeriodStartUtc { get; set; }
        public DateTime? PeriodEndUtc { get; set; }
    }
}
