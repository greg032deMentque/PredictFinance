namespace BackPredictFinance.ViewModels.ClientFinanceViewModels
{
    public class ClientDashboardViewModel
    {
        public decimal TotalPortfolioValue { get; set; }
        public decimal DayProfitLoss { get; set; }
        public int OpenPositions { get; set; }
        public int AnalysesThisWeek { get; set; }
        public int WatchlistCount { get; set; }
        public decimal RecommendationWinRate { get; set; }
        public DateTime NextMarketOpenAt { get; set; }
        public decimal TotalInvested { get; set; }
        public decimal TotalOutstanding { get; set; }
    }
}
