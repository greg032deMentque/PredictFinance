using BackPredictFinance.Common.enums;

namespace BackPredictFinance.Datas.Entities
{
    /// <summary>
    /// Conserve un scenario de wording deterministe rattache a une version publiee de gouvernance.
    /// </summary>
    public sealed class RecommendationWordingScenario : AuditableEntityBase
    {
        public string Id { get; set; } = string.Empty;
        public string WordingVersionId { get; set; } = string.Empty;
        public RecommendationWordingVersion WordingVersion { get; set; } = null!;
        public string ScenarioCode { get; set; } = string.Empty;
        public RecommendationKind RecommendationKind { get; set; }
        public HoldingStatusEnum HoldingStatus { get; set; }
        public string ActionVerbFamilyCode { get; set; } = string.Empty;
        public string RecommendationStrengthFamily { get; set; } = string.Empty;
        public string TemplateSummary { get; set; } = string.Empty;
    }
}
