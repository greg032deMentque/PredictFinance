using AutoMapper;
using BackPredictFinance.Datas.Entities;

namespace BackPredictFinance.ViewModels.ClientFinanceViewModels.Education
{
    public sealed class EducationArticleDetailViewModel
    {
        public string Slug { get; set; } = string.Empty;
        public string ProductType { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Summary { get; set; } = string.Empty;
        public string BodyMarkdown { get; set; } = string.Empty;
    }

    public sealed class EducationArticleDetailViewModelProfile : Profile
    {
        public EducationArticleDetailViewModelProfile()
        {
            CreateMap<EducationArticle, EducationArticleDetailViewModel>()
                .ForMember(dest => dest.ProductType, opt => opt.MapFrom(src => src.ProductType.ToString()));
        }
    }
}
