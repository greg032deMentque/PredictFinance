using BackPredictFinance.Common.enums;

namespace BackPredictFinance.Contracts.Analysis
{
    public sealed class AnalysisRecommendation
    {
        public string RecommendationId { get; set; } = string.Empty;
        public string SituationCode { get; set; } = string.Empty;
        public string AdviceScenarioCode { get; set; } = string.Empty;
        public RecommendationActionEnum RecommendationAction { get; set; } = RecommendationActionEnum.Wait;
        public RecommendationStrengthEnum RecommendationStrength { get; set; } = RecommendationStrengthEnum.Low;
        public AnalysisRecommendationParameters Parameters { get; set; } = new();
        public string HoldingContext { get; set; } = string.Empty;
        public string Rationale { get; set; } = string.Empty;
        public List<string> BasedOnPatternIds { get; set; } = [];
        public int? ReviewHorizonDays { get; set; }
        public string PolicyVersion { get; set; } = string.Empty;
        public string? WarningText { get; set; }
    }
}
