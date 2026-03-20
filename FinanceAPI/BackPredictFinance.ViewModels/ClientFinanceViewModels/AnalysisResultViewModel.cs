using AutoMapper;
using BackPredictFinance.Common.enums;
using BackPredictFinance.Datas.Entities;

namespace BackPredictFinance.ViewModels.ClientFinanceViewModels
{
    public class AnalysisResultViewModel
    {
        public string Id { get; set; } = string.Empty;
        public string Symbol { get; set; } = string.Empty;
        public string CompanyName { get; set; } = string.Empty;
        public TradingPatternEnum Pattern { get; set; } = TradingPatternEnum.DoubleTop;
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
                .ForMember(dest => dest.CompanyName, opt => opt.MapFrom(src => src.Asset.Name ?? src.Asset.Symbol))
                .ForMember(dest => dest.Pattern, opt => opt.MapFrom(src => GetPrimaryPattern(src)))
                .ForMember(dest => dest.Phase, opt => opt.MapFrom(src => GetPrimaryPhase(src)))
                .ForMember(dest => dest.Probability, opt => opt.MapFrom(src => GetProbability(src)))
                .ForMember(dest => dest.RecommendationAction, opt => opt.MapFrom(src => GetAction(src)))
                .ForMember(dest => dest.RecommendationReason, opt => opt.MapFrom(src => GetReason(src)))
                .ForMember(dest => dest.RiskLevel, opt => opt.MapFrom(src => GetRiskLevel(src)))
                .ForMember(dest => dest.RecommendationHorizonDays, opt => opt.MapFrom(src => src.DecisionSignal != null ? src.DecisionSignal.HorizonDays : 0))
                .ForMember(dest => dest.PredictedAt, opt => opt.MapFrom(src => src.CompletedAtUtc ?? src.StartedAtUtc))
                .ForMember(dest => dest.IsActionable, opt => opt.MapFrom(src => src.DecisionSignal != null && src.DecisionSignal.IsActionable))
                .ForMember(dest => dest.ModelStatus, opt => opt.MapFrom(src => src.ModelSnapshot != null ? src.ModelSnapshot.ModelStatus : ModelStatusEnum.NoGo))
                .ForMember(dest => dest.ModelMessage, opt => opt.MapFrom(src => GetModelMessage(src)))
                .ForMember(dest => dest.CurrentPrice, opt => opt.MapFrom(src => GetCurrentPrice(src)))
                .ForMember(dest => dest.NecklinePrice, opt => opt.MapFrom(src => GetNecklinePrice(src)))
                .ForMember(dest => dest.TargetPrice, opt => opt.MapFrom(src => GetTargetPrice(src)))
                .ForMember(dest => dest.InvalidationPrice, opt => opt.MapFrom(src => GetInvalidationPrice(src)));
        }

        private static PatternAssessment? GetPrimaryAssessment(AnalysisRun source)
            => source.PatternAssessments.OrderByDescending(x => x.IsPrimary).ThenBy(x => x.CreatedAtUtc).FirstOrDefault();

        private static decimal GetCurrentPrice(AnalysisRun source)
            => GetPrimaryAssessment(source)?.CurrentPrice ?? 0m;

        private static decimal? GetNecklinePrice(AnalysisRun source)
            => GetPrimaryAssessment(source)?.NecklinePrice;

        private static decimal? GetTargetPrice(AnalysisRun source)
            => GetPrimaryAssessment(source)?.TargetPrice;

        private static decimal? GetInvalidationPrice(AnalysisRun source)
            => GetPrimaryAssessment(source)?.InvalidationPrice;

        private static TradingPatternEnum GetPrimaryPattern(AnalysisRun source)
            => GetPrimaryAssessment(source)?.Pattern ?? source.RequestedPattern;

        private static string GetPrimaryPhase(AnalysisRun source)
            => GetPrimaryAssessment(source)?.Phase ?? string.Empty;

        private static decimal GetProbability(AnalysisRun source)
            => GetPrimaryAssessment(source)?.Probability ?? 0m;

        private static RecommendationActionEnum GetAction(AnalysisRun source)
            => source.DecisionSignal?.Action ?? RecommendationActionEnum.Hold;

        private static string GetReason(AnalysisRun source)
            => source.DecisionSignal?.Reason
                ?? (string.IsNullOrWhiteSpace(source.ErrorMessage) ? "Aucune justification" : source.ErrorMessage);

        private static RiskLevelEnum GetRiskLevel(AnalysisRun source)
        {
            var confidence = source.DecisionSignal?.Confidence ?? GetPrimaryAssessment(source)?.Confidence ?? 0m;
            var actionable = source.DecisionSignal?.IsActionable ?? false;

            if (!actionable)
            {
                return RiskLevelEnum.Information;
            }

            if (confidence >= 0.75m)
            {
                return RiskLevelEnum.Low;
            }

            if (confidence >= 0.45m)
            {
                return RiskLevelEnum.Moderate;
            }

            return RiskLevelEnum.High;
        }

        private static string GetModelMessage(AnalysisRun source)
            => source.ModelSnapshot?.ModelMessage
                ?? (string.IsNullOrWhiteSpace(source.ErrorMessage) ? string.Empty : source.ErrorMessage);

    }
}
