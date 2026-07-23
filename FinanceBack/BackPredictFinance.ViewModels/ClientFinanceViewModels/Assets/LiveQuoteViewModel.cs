using AutoMapper;
using BackPredictFinance.Datas.Entities;

namespace BackPredictFinance.ViewModels.ClientFinanceViewModels.Assets
{
    public class LiveQuoteViewModel
    {
        public string Symbol { get; set; } = string.Empty;
        public string AssetType { get; set; } = string.Empty;
        public decimal LastPrice { get; set; }
        public decimal DayVariationPct { get; set; }
        public string CompanyName { get; set; } = string.Empty;
        public DateTime AsOfUtc { get; set; }
    }

    public sealed class LiveQuoteViewModelProfile : Profile
    {
        public LiveQuoteViewModelProfile()
        {
            CreateMap<PriceHistory, LiveQuoteViewModel>()
                .ForMember(dest => dest.Symbol, opt => opt.MapFrom(src => src.Asset.Symbol))
                .ForMember(dest => dest.AssetType, opt => opt.MapFrom(src => src.Asset.AssetType.ToString()))
                .ForMember(dest => dest.CompanyName, opt => opt.MapFrom(src => src.Asset.Name ?? src.Asset.Symbol))
                .ForMember(dest => dest.LastPrice, opt => opt.MapFrom(src => src.Price))
                .ForMember(dest => dest.AsOfUtc, opt => opt.MapFrom(src => src.RetrievedAtUtc))
                .ForMember(dest => dest.DayVariationPct, opt => opt.Ignore());
        }
    }
}
