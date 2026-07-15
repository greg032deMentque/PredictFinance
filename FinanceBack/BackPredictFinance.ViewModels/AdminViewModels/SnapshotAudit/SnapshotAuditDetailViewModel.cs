namespace BackPredictFinance.ViewModels.AdminViewModels.SnapshotAudit
{
    public sealed class SnapshotAuditDetailViewModel : SnapshotAuditItemViewModel
    {
        public string? ErrorMessage { get; set; }
        public string RawPayload { get; set; } = string.Empty;
        public List<string> RequestedPatternIds { get; set; } = [];
        public string? RecommendationPolicyVersion { get; set; }
        public string? ExplanationPolicyVersion { get; set; }
        public string? ModelMessage { get; set; }
        public string? DecisionAction { get; set; }
        public string? DecisionSummary { get; set; }
    }
}
