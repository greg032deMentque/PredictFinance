using AutoMapper;

namespace BackPredictFinance.ViewModels.AdminViewModels.Wording
{
    public sealed class AdminWordingVersionListItemViewModel
    {
        public string WordingVersionId { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime? ActivatedAtUtc { get; set; }
        public int ScenarioCount { get; set; }
        public WordingPublicationStateViewModel PublicationState { get; set; } = new();
    }


}
