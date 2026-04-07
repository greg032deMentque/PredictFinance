using AutoMapper;
using BackPredictFinance.Datas.Entities;

namespace BackPredictFinance.ViewModels.ClientFinanceViewModels
{
    public sealed class AnalysisResultViewModelProfile : Profile
    {
        public AnalysisResultViewModelProfile()
        {
            CreateMap<AnalysisRun, AnalysisResultViewModel>()
                .ForMember(dest => dest.Symbol, opt => opt.MapFrom(src => src.Asset.Symbol))
                .ForMember(dest => dest.CompanyName, opt => opt.MapFrom(src => src.Asset.Name ?? src.Asset.Symbol));
        }
    }
}
