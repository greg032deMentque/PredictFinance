namespace BackPredictFinance.Common.AnalysisV1
{
    public sealed class AnalysisSnapshotPatternRow
    {
        public string SnapshotPatternRowId { get; set; } = string.Empty;
        public string SnapshotId { get; set; } = string.Empty;
        public string PatternId { get; set; } = string.Empty;
        public int DisplayRank { get; set; }
        public bool IsCompatible { get; set; }
        public bool IsPrimaryDisplayCandidate { get; set; }
        public PatternAssessment PatternAssessmentPayload { get; set; } = new();
    }
}
