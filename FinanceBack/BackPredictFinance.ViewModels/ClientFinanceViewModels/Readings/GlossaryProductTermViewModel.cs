using AutoMapper;
using BackPredictFinance.Datas.Entities;

namespace BackPredictFinance.ViewModels.ClientFinanceViewModels.Readings
{
    /// <summary>
    /// Terme du glossaire produits (PEA, PER, assurance vie...) exposé au client et à l'admin.
    /// </summary>
    public sealed class GlossaryProductTermViewModel
    {
        public string Term { get; set; } = string.Empty;
        public string Definition { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
    }

    public sealed class GlossaryProductTermViewModelProfile : Profile
    {
        public GlossaryProductTermViewModelProfile()
        {
            CreateMap<GlossaryTerm, GlossaryProductTermViewModel>()
                .ForMember(dest => dest.Category, opt => opt.MapFrom(src => src.Category.ToString()));
        }
    }
}
