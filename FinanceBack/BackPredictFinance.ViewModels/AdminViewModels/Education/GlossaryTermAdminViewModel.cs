using AutoMapper;
using BackPredictFinance.Datas.Entities;

namespace BackPredictFinance.ViewModels.AdminViewModels.Education
{
    public sealed class GlossaryTermAdminViewModel
    {
        public string Id { get; set; } = string.Empty;
        public string Term { get; set; } = string.Empty;
        public string Definition { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public bool IsPublished { get; set; }
    }

    public sealed class GlossaryTermAdminViewModelProfile : Profile
    {
        public GlossaryTermAdminViewModelProfile()
        {
            CreateMap<GlossaryTerm, GlossaryTermAdminViewModel>()
                .ForMember(dest => dest.Category, opt => opt.MapFrom(src => src.Category.ToString()));
        }
    }
}
