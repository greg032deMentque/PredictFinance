using BackPredictFinance.Common.enums;

namespace BackPredictFinance.Common.AnalysisV1
{
    public sealed class AnalysisRecommendation
    {
        public string RecommendationId { get; set; } = string.Empty;
        public RecommendationKind Kind { get; set; }
        public HoldingStatusEnum HoldingContext { get; set; } = HoldingStatusEnum.NotHeld;
        public string Rationale { get; set; } = string.Empty;
        public List<string> BasedOnPatternIds { get; set; } = [];
        public int? ReviewHorizonDays { get; set; }
        public string PolicyVersion { get; set; } = string.Empty;
        public string? WarningText { get; set; }
    }
}
