using AutoMapper;
using BackPredictFinance.Datas.Entities;

namespace BackPredictFinance.ViewModels.AdminViewModels.Content
{
    public sealed class LegalCardAdminViewModel
    {
        public string Id { get; set; } = string.Empty;
        public string Key { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime? EffectiveDate { get; set; }
        public string? TargetRoute { get; set; }
        public int DisplayOrder { get; set; }
        public bool IsPublished { get; set; }
    }

    public sealed class LegalCardAdminViewModelProfile : Profile
    {
        public LegalCardAdminViewModelProfile()
        {
            CreateMap<LegalCard, LegalCardAdminViewModel>();
        }
    }
}
