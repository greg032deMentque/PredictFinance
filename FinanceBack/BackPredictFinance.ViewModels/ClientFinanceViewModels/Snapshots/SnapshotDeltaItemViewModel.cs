namespace BackPredictFinance.ViewModels.ClientFinanceViewModels.Snapshots
{
    public sealed class SnapshotDeltaItemViewModel
    {
        public string FieldCode { get; set; } = string.Empty;
        public string DisplayLabel { get; set; } = string.Empty;
        public string? LeftValue { get; set; }
        public string? RightValue { get; set; }
        public string ChangeKind { get; set; } = string.Empty;
        public string EvidenceType { get; set; } = string.Empty;
    }
}
