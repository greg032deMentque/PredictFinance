using AutoMapper;
using BackPredictFinance.Datas.Entities;

namespace BackPredictFinance.ViewModels.AdminViewModels.Content
{
    public sealed class AnalysisConceptAdminViewModel
    {
        public string Code { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public string Explanation { get; set; } = string.Empty;
    }

    public sealed class AnalysisConceptAdminViewModelProfile : Profile
    {
        public AnalysisConceptAdminViewModelProfile()
        {
            CreateMap<AnalysisConceptExplanation, AnalysisConceptAdminViewModel>();
        }
    }
}
