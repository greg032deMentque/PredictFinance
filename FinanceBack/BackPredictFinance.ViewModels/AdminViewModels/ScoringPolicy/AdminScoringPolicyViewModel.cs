namespace BackPredictFinance.ViewModels.AdminViewModels.ScoringPolicy
{
    public sealed class AdminScoringPolicyViewModel
    {
        public string SupportedUniverseId { get; set; } = string.Empty;
        public string ScoringVersion { get; set; } = string.Empty;
        public string EligibilityPolicyVersion { get; set; } = string.Empty;
        public string ProviderId { get; set; } = string.Empty;
        public string AsOfUtcSemantics { get; set; } = string.Empty;
        public List<string> CategoryCodes { get; set; } = [];
        public List<string> MetricCodes { get; set; } = [];
        public List<string> HigherIsBetterMetricCodes { get; set; } = [];
        public List<string> LowerIsBetterMetricCodes { get; set; } = [];
        public int MinimumCategoriesRequiredFloor { get; set; }
        public int MinimumCategoriesRequiredCeiling { get; set; }
        public int MinimumCategoriesRequiredDefault { get; set; }
        public int MinimumSectorSampleSize { get; set; }
        public bool CoveragePenaltySupported { get; set; }
        public string? ActivePolicyVersionId { get; set; }
    }
}
