namespace BackPredictFinance.Common.AdminGovernance
{
    public sealed class AdminWordingVersionListItem
    {
        public string WordingVersionId { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime? ActivatedAtUtc { get; set; }
        public int ScenarioCount { get; set; }
        public WordingPublicationState PublicationState { get; set; } = new();
    }
}
