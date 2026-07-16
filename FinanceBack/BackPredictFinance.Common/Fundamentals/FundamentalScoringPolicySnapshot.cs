namespace BackPredictFinance.Common.Fundamentals
{
    public sealed class FundamentalScoringPolicySnapshot
    {
        public string? PolicyVersionId { get; set; }
        public bool IsActivePolicyPresent { get; set; }
        public int MinimumCategoriesRequiredFloor { get; set; }
        public int MinimumCategoriesRequiredCeiling { get; set; }
        public int MinimumCategoriesRequiredDefault { get; set; }
        public int MinimumSectorSampleSize { get; set; }
        public bool CoveragePenaltySupported { get; set; }

        public static FundamentalScoringPolicySnapshot Defaults()
        {
            return new FundamentalScoringPolicySnapshot
            {
                PolicyVersionId = null,
                IsActivePolicyPresent = false,
                MinimumCategoriesRequiredFloor = FundamentalScoringPolicyDefaults.MinimumCategoriesRequiredFloor,
                MinimumCategoriesRequiredCeiling = FundamentalScoringPolicyDefaults.MinimumCategoriesRequiredCeiling,
                MinimumCategoriesRequiredDefault = FundamentalScoringPolicyDefaults.MinimumCategoriesRequiredDefault,
                MinimumSectorSampleSize = FundamentalScoringPolicyDefaults.MinimumSectorSampleSize,
                CoveragePenaltySupported = FundamentalScoringPolicyDefaults.CoveragePenaltySupported
            };
        }
    }
}
