using AutoMapper;
using BackPredictFinance.Datas.Entities;

namespace BackPredictFinance.ViewModels.ClientFinanceViewModels.Education
{
    public sealed class EducationArticleSummaryViewModel
    {
        public string Slug { get; set; } = string.Empty;
        public string ProductType { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Summary { get; set; } = string.Empty;
        public int DisplayOrder { get; set; }
    }

    public sealed class EducationArticleSummaryViewModelProfile : Profile
    {
        public EducationArticleSummaryViewModelProfile()
        {
            CreateMap<EducationArticle, EducationArticleSummaryViewModel>()
                .ForMember(dest => dest.ProductType, opt => opt.MapFrom(src => src.ProductType.ToString()));
        }
    }
}
