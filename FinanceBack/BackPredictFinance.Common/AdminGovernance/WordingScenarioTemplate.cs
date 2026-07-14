using BackPredictFinance.Common.enums;

namespace BackPredictFinance.Common.AdminGovernance
{
    public sealed class WordingScenarioTemplate
    {
        public string ScenarioCode { get; set; } = string.Empty;
        public RecommendationKind RecommendationKind { get; set; }
        public HoldingStatusEnum HoldingStatus { get; set; }
        public string ActionVerbFamilyCode { get; set; } = string.Empty;
        public List<RecommendationStrengthEnum> SupportedStrengths { get; set; } = [];
        public string TemplateSummary { get; set; } = string.Empty;
    }
}
