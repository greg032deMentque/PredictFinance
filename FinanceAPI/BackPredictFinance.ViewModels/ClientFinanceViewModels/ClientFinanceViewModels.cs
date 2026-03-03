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

    public class AssetSearchItemViewModel
    {
        public string Symbol { get; set; } = string.Empty;
        public string CompanyName { get; set; } = string.Empty;
        public string Market { get; set; } = string.Empty;
        public string Currency { get; set; } = "USD";
        public decimal LastPrice { get; set; }
        public decimal DayVariationPct { get; set; }
    }

    public class WatchlistUpsertRequestViewModel
    {
        public string Symbol { get; set; } = string.Empty;
        public string CompanyName { get; set; } = string.Empty;
        public string Market { get; set; } = string.Empty;
    }

    public class WatchlistItemViewModel
    {
        public string UserAssetId { get; set; } = string.Empty;
        public string Symbol { get; set; } = string.Empty;
        public string CompanyName { get; set; } = string.Empty;
        public string Market { get; set; } = string.Empty;
        public decimal LastPrice { get; set; }
        public decimal DayVariationPct { get; set; }
        public decimal HeldQuantity { get; set; }
        public decimal AverageBuyPrice { get; set; }
        public decimal InvestedAmount { get; set; }
        public decimal OutstandingAmount { get; set; }
    }

    public class LiveQuoteViewModel
    {
        public string Symbol { get; set; } = string.Empty;
        public decimal LastPrice { get; set; }
        public decimal DayVariationPct { get; set; }
        public DateTime AsOfUtc { get; set; }
    }

    public class TransactionCreateRequestViewModel
    {
        public string Symbol { get; set; } = string.Empty;
        public string TransactionType { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal Fees { get; set; }
        public DateTime? TimestampUtc { get; set; }
    }

    public class TransactionItemViewModel
    {
        public string Id { get; set; } = string.Empty;
        public string Symbol { get; set; } = string.Empty;
        public string CompanyName { get; set; } = string.Empty;
        public string TransactionType { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal Fees { get; set; }
        public decimal GrossAmount { get; set; }
        public decimal NetAmount { get; set; }
        public DateTime TimestampUtc { get; set; }
    }

    public class AnalysisRunRequestViewModel
    {
        public string Symbol { get; set; } = string.Empty;
    }

    public class AnalysisResultViewModel
    {
        public string Id { get; set; } = string.Empty;
        public string Symbol { get; set; } = string.Empty;
        public string CompanyName { get; set; } = string.Empty;
        public string Pattern { get; set; } = string.Empty;
        public decimal Confidence { get; set; }
        public string Recommendation { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
        public string RiskLevel { get; set; } = string.Empty;
        public int HorizonDays { get; set; }
        public DateTime PredictedAt { get; set; }
    }

    public class SimulationRequestViewModel
    {
        public string Symbol { get; set; } = string.Empty;
        public decimal InvestmentAmount { get; set; }
        public int HorizonDays { get; set; }
    }

    public class SimulationResultViewModel
    {
        public string Symbol { get; set; } = string.Empty;
        public decimal InvestmentAmount { get; set; }
        public int HorizonDays { get; set; }
        public decimal EstimatedReturnAmount { get; set; }
        public decimal EstimatedReturnPct { get; set; }
        public decimal EstimatedFinalAmount { get; set; }
        public string Recommendation { get; set; } = string.Empty;
        public string Assumption { get; set; } = string.Empty;
    }
}
