using AutoMapper;
using BackPredictFinance.Datas.Entities;

namespace BackPredictFinance.ViewModels.AdminViewModels.Content
{
    public sealed class FaqEntryAdminViewModel
    {
        public string Id { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Question { get; set; } = string.Empty;
        public string Answer { get; set; } = string.Empty;
        public int DisplayOrder { get; set; }
        public bool IsPublished { get; set; }
    }

    public sealed class FaqEntryAdminViewModelProfile : Profile
    {
        public FaqEntryAdminViewModelProfile()
        {
            CreateMap<FaqEntry, FaqEntryAdminViewModel>();
        }
    }
}
