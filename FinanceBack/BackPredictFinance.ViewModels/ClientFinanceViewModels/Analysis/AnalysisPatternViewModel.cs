using BackPredictFinance.Common.enums;

namespace BackPredictFinance.ViewModels.ClientFinanceViewModels.Analysis
{
    public sealed class AnalysisPatternViewModel
    {
        public decimal? AtrStopLossPrice { get; set; }
        public decimal? AtrTarget1Price { get; set; }
        public decimal? AtrTarget2Price { get; set; }
        public decimal? AtrRiskRewardRatio { get; set; }
        public decimal? PositionSizePercent { get; set; }
        public decimal VolumeRatio { get; set; }
        public VolumeConfirmation VolumeConfirmation { get; set; }
        public string PatternId { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string PedagogicalDescription { get; set; } = string.Empty;
        public string PhaseCode { get; set; } = string.Empty;
        public string PhaseLabel { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public bool IsCompatible { get; set; }
        public decimal ConfidenceScore { get; set; }
        public string ConfidenceLabel { get; set; } = string.Empty;
        public decimal? ProbabilityScore { get; set; }
        public string? ProbabilityLabel { get; set; }
        public bool IsCredible { get; set; }
        public List<string> ScoreReasons { get; set; } = [];
        public decimal CurrentPrice { get; set; }
        public decimal? NecklinePrice { get; set; }
        public string ValidationState { get; set; } = string.Empty;
        public decimal? ValidationLevel { get; set; }
        public DateOnly? ValidationDate { get; set; }
        public string InvalidationState { get; set; } = string.Empty;
        public decimal? InvalidationLevel { get; set; }
        public DateOnly? InvalidationDate { get; set; }
        public bool HasRiskPlan { get; set; }
        public decimal? SuggestedStopLoss { get; set; }
        public decimal? SuggestedTakeProfit { get; set; }
        public decimal? RiskRewardRatio { get; set; }
        public string? PositioningNote { get; set; }
        public List<StructuralPointViewModel> StructuralPoints { get; set; } = [];
        public string WhyListed { get; set; } = string.Empty;
        public string PedagogicalSummary { get; set; } = string.Empty;
        public string? AmbiguityNote { get; set; }
        public string? LimitationsNote { get; set; }
        public bool IsActionable { get; set; }
        public string RecommendationAction { get; set; } = string.Empty;
        public string RecommendationReason { get; set; } = string.Empty;
        public string RiskLevel { get; set; } = string.Empty;
        public int RecommendationHorizonDays { get; set; }
    }
}
