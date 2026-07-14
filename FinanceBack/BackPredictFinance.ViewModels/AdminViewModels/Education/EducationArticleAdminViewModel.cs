using AutoMapper;
using BackPredictFinance.Datas.Entities;

namespace BackPredictFinance.ViewModels.AdminViewModels.Education
{
    public sealed class EducationArticleAdminViewModel
    {
        public string Id { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string ProductType { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Summary { get; set; } = string.Empty;
        public string BodyMarkdown { get; set; } = string.Empty;
        public int DisplayOrder { get; set; }
        public bool IsPublished { get; set; }
    }

    public sealed class EducationArticleAdminViewModelProfile : Profile
    {
        public EducationArticleAdminViewModelProfile()
        {
            CreateMap<EducationArticle, EducationArticleAdminViewModel>()
                .ForMember(dest => dest.ProductType, opt => opt.MapFrom(src => src.ProductType.ToString()));
        }
    }
}
