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
        public string Pattern { get; set; } = string.Empty;
        public string Phase { get; set; } = string.Empty;
        public decimal Confidence { get; set; }
        public string Recommendation { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
        public string RiskLevel { get; set; } = string.Empty;
        public int HorizonDays { get; set; }
        public DateTime PredictedAt { get; set; }
        public bool IsActionable { get; set; }
        public string ModelStatus { get; set; } = string.Empty;
        public string ModelMessage { get; set; } = string.Empty;
        public decimal CurrentPrice { get; set; }
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
                .ForMember(dest => dest.Confidence, opt => opt.MapFrom(src => GetConfidence(src)))
                .ForMember(dest => dest.Recommendation, opt => opt.MapFrom(src => GetAction(src)))
                .ForMember(dest => dest.Reason, opt => opt.MapFrom(src => src.DecisionSignal != null ? src.DecisionSignal.Reason : "Aucune justification"))
                .ForMember(dest => dest.RiskLevel, opt => opt.MapFrom(src => InferRiskLevel(GetConfidence(src), src.DecisionSignal != null && src.DecisionSignal.IsActionable)))
                .ForMember(dest => dest.HorizonDays, opt => opt.MapFrom(src => src.DecisionSignal != null ? src.DecisionSignal.HorizonDays : 0))
                .ForMember(dest => dest.PredictedAt, opt => opt.MapFrom(src => src.CompletedAtUtc ?? src.StartedAtUtc))
                .ForMember(dest => dest.IsActionable, opt => opt.MapFrom(src => src.DecisionSignal != null && src.DecisionSignal.IsActionable))
                .ForMember(dest => dest.ModelStatus, opt => opt.MapFrom(src => src.ModelSnapshot != null ? src.ModelSnapshot.ModelStatus.ToString() : ModelStatusEnum.NoGo.ToString()))
                .ForMember(dest => dest.ModelMessage, opt => opt.MapFrom(src => src.ModelSnapshot != null ? src.ModelSnapshot.ModelMessage : string.Empty))
                .ForMember(dest => dest.CurrentPrice, opt => opt.MapFrom(src => GetCurrentPrice(src)))
                .ForMember(dest => dest.TargetPrice, opt => opt.MapFrom(src => GetTargetPrice(src)))
                .ForMember(dest => dest.InvalidationPrice, opt => opt.MapFrom(src => GetInvalidationPrice(src)));
        }

        private static PatternAssessment? GetPrimaryAssessment(AnalysisRun source)
            => source.PatternAssessments.OrderByDescending(x => x.IsPrimary).ThenBy(x => x.CreatedAtUtc).FirstOrDefault();

        private static decimal GetCurrentPrice(AnalysisRun source)
            => GetPrimaryAssessment(source)?.CurrentPrice ?? 0m;

        private static decimal? GetTargetPrice(AnalysisRun source)
            => GetPrimaryAssessment(source)?.TargetPrice;

        private static decimal? GetInvalidationPrice(AnalysisRun source)
            => GetPrimaryAssessment(source)?.InvalidationPrice;

        private static string GetPrimaryPattern(AnalysisRun source)
            => FormatPattern(GetPrimaryAssessment(source)?.Pattern ?? TradingPatternEnum.DoubleTop);

        private static string GetPrimaryPhase(AnalysisRun source)
            => GetPrimaryAssessment(source)?.Phase ?? string.Empty;

        private static decimal GetConfidence(AnalysisRun source)
            => source.DecisionSignal?.Confidence ?? GetPrimaryAssessment(source)?.Confidence ?? 0m;

        private static string GetAction(AnalysisRun source)
            => FormatAction(source.DecisionSignal?.Action ?? RecommendationActionEnum.NonActionable);

        private static string FormatPattern(TradingPatternEnum pattern)
        {
            return pattern switch
            {
                TradingPatternEnum.HeadAndShoulders => "HEAD_AND_SHOULDERS",
                TradingPatternEnum.DoubleTop => "DOUBLE_TOP",
                TradingPatternEnum.DoubleBottom => "DOUBLE_BOTTOM",
                TradingPatternEnum.CupAndHandle => "CUP_AND_HANDLE",
                TradingPatternEnum.Triangle => "TRIANGLE",
                _ => "DOUBLE_TOP"
            };
        }

        private static string FormatAction(RecommendationActionEnum action)
        {
            return action switch
            {
                RecommendationActionEnum.Buy => "buy",
                RecommendationActionEnum.Sell => "sell",
                RecommendationActionEnum.Hold => "hold",
                RecommendationActionEnum.NonActionable => "non_actionable",
                _ => "hold"
            };
        }

        private static string InferRiskLevel(decimal confidence, bool actionable)
        {
            if (!actionable)
            {
                return "Information";
            }

            if (confidence >= 0.75m)
            {
                return "Faible";
            }

            if (confidence >= 0.45m)
            {
                return "Modere";
            }

            return "Eleve";
        }
    }
}
