using BackPredictFinance.Common.enums;

namespace BackPredictFinance.ViewModels.AdminViewModels.SnapshotAudit
{
    public sealed class SnapshotAuditComparisonViewModel
    {
        public string LeftAnalysisRunId { get; set; } = string.Empty;
        public string RightAnalysisRunId { get; set; } = string.Empty;
        public bool SameUser { get; set; }
        public bool SameAsset { get; set; }
        public bool SamePrimaryPattern { get; set; }
        public bool SameRecommendationAction { get; set; }
        public string? LeftPrimaryPatternId { get; set; }
        public string? RightPrimaryPatternId { get; set; }
        public string? LeftRecommendationAction { get; set; }
        public string? RightRecommendationAction { get; set; }
        public string? LeftAnalysisEngineVersion { get; set; }
        public string? RightAnalysisEngineVersion { get; set; }
        public ModelStatusEnum? LeftModelStatus { get; set; }
        public ModelStatusEnum? RightModelStatus { get; set; }
    }
}
