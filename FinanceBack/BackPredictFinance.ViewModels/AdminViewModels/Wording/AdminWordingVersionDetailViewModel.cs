using AutoMapper;
using BackPredictFinance.Common.AdminGovernance;

namespace BackPredictFinance.ViewModels.AdminViewModels.Wording
{
    public sealed class AdminWordingVersionDetailViewModel
    {
        public string WordingVersionId { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public WordingPublicationStateViewModel PublicationState { get; set; } = new();
        public WordingScenarioTemplateSummaryViewModel Scenario { get; set; } = new();
    }

    public sealed class AdminWordingVersionDetailViewModelProfile : Profile
    {
        public AdminWordingVersionDetailViewModelProfile()
        {
            CreateMap<AdminWordingVersionDetail, AdminWordingVersionDetailViewModel>();
            CreateMap<AdminWordingVersionListItem, AdminWordingVersionListItemViewModel>();
            CreateMap<WordingPublicationState, WordingPublicationStateViewModel>();
            CreateMap<WordingScenarioTemplate, WordingScenarioTemplateSummaryViewModel>();
        }
    }
}
