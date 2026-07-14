using AutoMapper;
using BackPredictFinance.Datas.Entities;

namespace BackPredictFinance.ViewModels.ClientFinanceViewModels.Content
{
    public sealed class LegalCardViewModel
    {
        public string Icon { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime? UpdatedAt { get; set; }
        public string? RouterLink { get; set; }
        public int DisplayOrder { get; set; }
    }

    public sealed class LegalCardViewModelProfile : Profile
    {
        public LegalCardViewModelProfile()
        {
            CreateMap<LegalCard, LegalCardViewModel>()
                .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => src.EffectiveDate))
                .ForMember(dest => dest.RouterLink, opt => opt.MapFrom(src => src.TargetRoute));
        }
    }
}
