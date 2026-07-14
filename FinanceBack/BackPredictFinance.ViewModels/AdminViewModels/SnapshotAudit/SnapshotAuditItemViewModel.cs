using BackPredictFinance.Common.enums;

namespace BackPredictFinance.ViewModels.AdminViewModels.SnapshotAudit
{
    public sealed class SnapshotAuditItemViewModel
    {
        public string AnalysisRunId { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string AssetId { get; set; } = string.Empty;
        public string Symbol { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime StartedAtUtc { get; set; }
        public DateTime? CompletedAtUtc { get; set; }
        public string TraceId { get; set; } = string.Empty;
        public string? PrimaryPatternId { get; set; }
        public List<string> ExecutedPatternIds { get; set; } = [];
        public string? RecommendationAction { get; set; }
        public ModelStatusEnum? ModelStatus { get; set; }
        public string? AnalysisEngineVersion { get; set; }
    }
}
