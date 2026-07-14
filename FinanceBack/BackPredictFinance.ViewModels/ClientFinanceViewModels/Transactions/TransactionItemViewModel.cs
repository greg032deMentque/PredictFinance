using AutoMapper;
using BackPredictFinance.Common.enums;
using BackPredictFinance.Datas.Entities;

namespace BackPredictFinance.ViewModels.ClientFinanceViewModels.Transactions
{
    public class TransactionItemViewModel
    {
        public string Id { get; set; } = string.Empty;
        public string Symbol { get; set; } = string.Empty;
        public string CompanyName { get; set; } = string.Empty;
        public string TransactionType { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal Fees { get; set; }
        public decimal GrossAmount { get; set; }
        public decimal NetAmount { get; set; }
        public DateTime TimestampUtc { get; set; }
        public string PortfolioId { get; set; } = string.Empty;
        public string PortfolioName { get; set; } = string.Empty;
    }

    public sealed class TransactionItemViewModelProfile : Profile
    {
        public TransactionItemViewModelProfile()
        {
            CreateMap<AssetTransaction, TransactionItemViewModel>()
                .ForMember(dest => dest.Symbol, opt => opt.MapFrom(src => src.UserAsset.Asset.Symbol))
                .ForMember(dest => dest.CompanyName, opt => opt.MapFrom(src => src.UserAsset.Asset.Name ?? src.UserAsset.Asset.Symbol))
                .ForMember(dest => dest.TransactionType, opt => opt.MapFrom(src => src.TransactionType.ToString()))
                .ForMember(dest => dest.GrossAmount, opt => opt.MapFrom(src => decimal.Round(src.Quantity * src.UnitPrice, 2)))
                .ForMember(dest => dest.NetAmount, opt => opt.MapFrom(src =>
                    decimal.Round(
                        src.TransactionType == TransactionTypeEnum.Buy
                            ? (src.Quantity * src.UnitPrice) + src.Fees
                            : (src.Quantity * src.UnitPrice) - src.Fees,
                        2)))
                .ForMember(dest => dest.PortfolioId, opt => opt.MapFrom(src => src.PortfolioId))
                .ForMember(dest => dest.PortfolioName, opt => opt.MapFrom(src => src.Portfolio != null ? src.Portfolio.Name : string.Empty));
        }
    }
}
