namespace BackPredictFinance.Datas.Entities
{
    public sealed class FundamentalScoringPolicyVersion : AuditableEntityBase
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string DisplayName { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime? ActivatedAtUtc { get; set; }
        public int MinimumCategoriesRequiredFloor { get; set; }
        public int MinimumCategoriesRequiredCeiling { get; set; }
        public int MinimumCategoriesRequiredDefault { get; set; }
        public int MinimumSectorSampleSize { get; set; }
        public bool CoveragePenaltySupported { get; set; }
    }
}
