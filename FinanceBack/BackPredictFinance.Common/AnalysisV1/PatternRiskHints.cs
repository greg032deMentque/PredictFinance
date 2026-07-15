namespace BackPredictFinance.Common.AnalysisV1
{
    public sealed class PatternRiskHints
    {
        public bool HasRiskPlan { get; set; }
        public decimal? SuggestedStopLoss { get; set; }
        public decimal? SuggestedTakeProfit { get; set; }
        public decimal? RiskRewardRatio { get; set; }
        public string? PositioningNote { get; set; }
        public decimal? AtrStopLossPrice { get; set; }
        public decimal? AtrTarget1Price { get; set; }
        public decimal? AtrTarget2Price { get; set; }
        public decimal? AtrRiskRewardRatio { get; set; }
        public decimal? PositionSizePercent { get; set; }
    }
}
