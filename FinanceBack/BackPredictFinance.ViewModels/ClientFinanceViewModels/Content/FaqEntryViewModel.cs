using AutoMapper;
using BackPredictFinance.Datas.Entities;

namespace BackPredictFinance.ViewModels.ClientFinanceViewModels.Content
{
    public sealed class FaqEntryViewModel
    {
        public string Category { get; set; } = string.Empty;
        public string Question { get; set; } = string.Empty;
        public string Answer { get; set; } = string.Empty;
        public int DisplayOrder { get; set; }
    }

    public sealed class FaqEntryViewModelProfile : Profile
    {
        public FaqEntryViewModelProfile()
        {
            CreateMap<FaqEntry, FaqEntryViewModel>();
        }
    }
}
