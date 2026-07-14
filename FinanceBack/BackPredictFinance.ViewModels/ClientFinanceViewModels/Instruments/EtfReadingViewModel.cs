using AutoMapper;
using BackPredictFinance.Common.MarketData;

namespace BackPredictFinance.ViewModels.ClientFinanceViewModels.Instruments
{
    public sealed class EtfReadingViewModel
    {
        public string Symbol { get; set; } = string.Empty;
        public string? FundFamily { get; set; }
        public string? Category { get; set; }
        public string? LegalType { get; set; }
        public string? IndexTracked { get; set; }
        public string TotalExpenseRatio { get; set; } = "non disponible";
        public string TotalAssets { get; set; } = "non disponible";
        public string? ReplicationMethod { get; set; }
        public string YtdReturn { get; set; } = "non disponible";
        public string ThreeYearAverageReturn { get; set; } = "non disponible";
        public string FiveYearAverageReturn { get; set; } = "non disponible";
    }

    public sealed class EtfReadingViewModelProfile : Profile
    {
        public EtfReadingViewModelProfile()
        {
            CreateMap<MarketEtfProfileData, EtfReadingViewModel>()
                .ForMember(dest => dest.TotalExpenseRatio, opt => opt.MapFrom(src =>
                    src.TotalExpenseRatio.HasValue
                        ? $"{src.TotalExpenseRatio.Value:P2}"
                        : "non disponible"))
                .ForMember(dest => dest.TotalAssets, opt => opt.MapFrom(src =>
                    src.TotalAssets.HasValue
                        ? $"{src.TotalAssets.Value:N0}"
                        : "non disponible"))
                .ForMember(dest => dest.YtdReturn, opt => opt.MapFrom(src =>
                    src.YtdReturn.HasValue
                        ? $"{src.YtdReturn.Value:P2}"
                        : "non disponible"))
                .ForMember(dest => dest.ThreeYearAverageReturn, opt => opt.MapFrom(src =>
                    src.ThreeYearAverageReturn.HasValue
                        ? $"{src.ThreeYearAverageReturn.Value:P2}"
                        : "non disponible"))
                .ForMember(dest => dest.FiveYearAverageReturn, opt => opt.MapFrom(src =>
                    src.FiveYearAverageReturn.HasValue
                        ? $"{src.FiveYearAverageReturn.Value:P2}"
                        : "non disponible"));
        }
    }
}
