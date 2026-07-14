namespace BackPredictFinance.Common.Fundamentals
{
    public sealed class FundamentalScoreResponse
    {
        public string UniverseId { get; set; } = string.Empty;
        public string ScoringVersion { get; set; } = string.Empty;
        public string EligibilityPolicyVersion { get; set; } = string.Empty;
        public string ProviderId { get; set; } = string.Empty;
        public DateTime AsOfUtc { get; set; }
        public string AsOfUtcSemantics { get; set; } = string.Empty;
        public string? DataSnapshotId { get; set; }
        public List<FundamentalScoreResult> Results { get; set; } = [];
    }
}
