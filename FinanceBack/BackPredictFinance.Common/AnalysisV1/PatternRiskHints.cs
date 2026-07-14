namespace BackPredictFinance.Common.AnalysisV1
{
    public sealed class PatternRiskHints
    {
        public bool HasRiskPlan { get; set; }
        public decimal? SuggestedStopLoss { get; set; }
        public decimal? SuggestedTakeProfit { get; set; }
        public decimal? RiskRewardRatio { get; set; }
        public string? PositioningNote { get; set; }
    }
}
