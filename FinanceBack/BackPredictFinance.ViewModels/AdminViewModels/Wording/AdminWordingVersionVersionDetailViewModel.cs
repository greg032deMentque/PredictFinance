namespace BackPredictFinance.ViewModels.AdminViewModels.Wording
{
    public sealed class AdminWordingVersionVersionDetailViewModel
    {
        public string WordingVersionId { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public WordingPublicationStateViewModel PublicationState { get; set; } = new();
        public List<WordingScenarioTemplateSummaryViewModel> Scenarios { get; set; } = [];
    }
}
