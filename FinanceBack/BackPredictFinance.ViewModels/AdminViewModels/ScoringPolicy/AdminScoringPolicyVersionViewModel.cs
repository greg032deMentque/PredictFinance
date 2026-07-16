namespace BackPredictFinance.ViewModels.AdminViewModels.ScoringPolicy
{
    public sealed class AdminScoringPolicyVersionViewModel
    {
        public string Id { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime? ActivatedAtUtc { get; set; }
        public int MinimumCategoriesRequiredFloor { get; set; }
        public int MinimumCategoriesRequiredCeiling { get; set; }
        public int MinimumCategoriesRequiredDefault { get; set; }
        public int MinimumSectorSampleSize { get; set; }
        public bool CoveragePenaltySupported { get; set; }
        public DateTime CreatedAtUtc { get; set; }
    }
}
