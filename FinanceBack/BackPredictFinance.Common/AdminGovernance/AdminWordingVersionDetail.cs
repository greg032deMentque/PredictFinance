namespace BackPredictFinance.Common.AdminGovernance
{
    public sealed class AdminWordingVersionDetail
    {
        public string WordingVersionId { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public WordingPublicationState PublicationState { get; set; } = new();
        public WordingScenarioTemplate Scenario { get; set; } = new();
    }
}
