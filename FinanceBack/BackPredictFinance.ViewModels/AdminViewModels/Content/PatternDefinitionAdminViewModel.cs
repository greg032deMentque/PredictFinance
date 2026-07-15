using AutoMapper;
using BackPredictFinance.Datas.Entities;

namespace BackPredictFinance.ViewModels.AdminViewModels.Content
{
    public sealed class PatternDefinitionAdminViewModel
    {
        public string PatternId { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Family { get; set; } = string.Empty;
        public string FamilyLabel { get; set; } = string.Empty;
        public string Direction { get; set; } = string.Empty;
        public string DirectionLabel { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string AnalysisNarrative { get; set; } = string.Empty;
        public decimal Reliability { get; set; }
        public string ReliabilityLabel { get; set; } = string.Empty;
    }

    public sealed class PatternDefinitionAdminViewModelProfile : Profile
    {
        public PatternDefinitionAdminViewModelProfile()
        {
            CreateMap<PatternDefinition, PatternDefinitionAdminViewModel>();
        }
    }
}
