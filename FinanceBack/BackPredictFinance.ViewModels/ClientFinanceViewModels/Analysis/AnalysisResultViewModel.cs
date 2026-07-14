using AutoMapper;
using BackPredictFinance.Common.enums;
using BackPredictFinance.Datas.Entities;

namespace BackPredictFinance.ViewModels.ClientFinanceViewModels.Analysis
{
    public class AnalysisResultViewModel
    {
        public string Id { get; set; } = string.Empty;
        public string Symbol { get; set; } = string.Empty;
        public string CompanyName { get; set; } = string.Empty;
        public string Pattern { get; set; } = string.Empty;
        public string Phase { get; set; } = string.Empty;
        public decimal Probability { get; set; }
        public RecommendationActionEnum RecommendationAction { get; set; } = RecommendationActionEnum.Hold;
        public string RecommendationReason { get; set; } = string.Empty;
        public RiskLevelEnum RiskLevel { get; set; } = RiskLevelEnum.Information;
        public int RecommendationHorizonDays { get; set; }
        public DateTime PredictedAt { get; set; }
        public bool IsActionable { get; set; }
        public ModelStatusEnum ModelStatus { get; set; } = ModelStatusEnum.NoGo;
        public string ModelMessage { get; set; } = string.Empty;
        public decimal CurrentPrice { get; set; }
        public decimal? NecklinePrice { get; set; }
        public decimal? TargetPrice { get; set; }
        public decimal? InvalidationPrice { get; set; }
    }

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
