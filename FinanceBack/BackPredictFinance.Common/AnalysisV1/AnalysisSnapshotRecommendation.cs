namespace BackPredictFinance.Common.AnalysisV1
{
    public sealed class AnalysisSnapshotRecommendation
    {
        public string SnapshotRecommendationId { get; set; } = string.Empty;
        public string SnapshotId { get; set; } = string.Empty;
        public AnalysisRecommendation RecommendationPayload { get; set; } = new();
        public DateTime CreatedAtUtc { get; set; }
    }
}
