using AutoMapper;
using BackPredictFinance.Common.enums;
using DataEntities = BackPredictFinance.Datas.Entities;

namespace BackPredictFinance.ViewModels.ClientFinanceViewModels.Portfolios
{
    public sealed class UserPortfolioViewModel
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string PortfolioType { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }

    public sealed class UserPortfolioViewModelProfile : Profile
    {
        public UserPortfolioViewModelProfile()
        {
            CreateMap<DataEntities.Portfolio, UserPortfolioViewModel>()
                .ForMember(dest => dest.PortfolioType, opt => opt.MapFrom(src => src.PortfolioType.ToString()))
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()));
        }
    }
}
