using BackPredictFinance.Common.enums;

namespace BackPredictFinance.Contracts.Trading
{
    public sealed class TradingRecommendationResult
    {
        public RecommendationActionEnum Action { get; set; }
        public bool IsActionable { get; set; }
        public decimal Confidence { get; set; }
        public int HorizonDays { get; set; }
        public RiskLevelEnum RiskLevel { get; set; } = RiskLevelEnum.Information;
        public string Reason { get; set; } = string.Empty;
        public decimal? TargetPrice { get; set; }
        public decimal? InvalidationPrice { get; set; }
    }
}
