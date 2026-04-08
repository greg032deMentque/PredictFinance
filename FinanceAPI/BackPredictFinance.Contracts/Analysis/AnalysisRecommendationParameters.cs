namespace BackPredictFinance.Contracts.Analysis
{
    public sealed class AnalysisRecommendationParameters
    {
        public bool HoldsInstrument { get; set; }
        public bool IsActionable { get; set; }
        public bool HasMultipleCompatiblePatterns { get; set; }
        public bool HasRiskPlan { get; set; }
        public string? PrimaryPatternId { get; set; }
        public string? PrimaryPatternDisplayName { get; set; }
        public string? PatternFamilyId { get; set; }
        public string? BiasCode { get; set; }
        public string? CurrentPhaseCode { get; set; }
        public string? CurrentPhaseLabel { get; set; }
        public string? ValidationState { get; set; }
        public string? InvalidationState { get; set; }
        public decimal ConfidenceScore { get; set; }
        public string? ConfidenceLabel { get; set; }
        public decimal? SuggestedStopLoss { get; set; }
        public decimal? SuggestedTakeProfit { get; set; }
        public decimal? InvalidationLevel { get; set; }
        public decimal? RiskRewardRatio { get; set; }
    }
}
