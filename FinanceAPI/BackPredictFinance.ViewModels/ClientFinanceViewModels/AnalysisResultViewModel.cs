using System;
using AutoMapper;
using BackPredictFinance.Datas.Entities;

namespace BackPredictFinance.ViewModels.ClientFinanceViewModels
{
    public class AnalysisResultViewModel
    {
        public string Id { get; set; } = string.Empty;
        public string Symbol { get; set; } = string.Empty;
        public string CompanyName { get; set; } = string.Empty;
        public string Pattern { get; set; } = string.Empty;
        public decimal Confidence { get; set; }
        public string Recommendation { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
        public string RiskLevel { get; set; } = string.Empty;
        public int HorizonDays { get; set; }
        public DateTime PredictedAt { get; set; }
    }

    public sealed class AnalysisResultViewModelProfile : Profile
    {
        public AnalysisResultViewModelProfile()
        {
            CreateMap<Recommendation, AnalysisResultViewModel>()
                .ForMember(dest => dest.Symbol, opt => opt.MapFrom(src => src.UserAsset.Asset.Symbol))
                .ForMember(dest => dest.CompanyName, opt => opt.MapFrom(src => src.UserAsset.Asset.Name ?? src.UserAsset.Asset.Symbol))
                .ForMember(dest => dest.Pattern, opt => opt.MapFrom(src => ExtractPattern(src.Reason)))
                .ForMember(dest => dest.Recommendation, opt => opt.MapFrom(src => src.Action.ToString()))
                .ForMember(dest => dest.Reason, opt => opt.MapFrom(src => ExtractReason(src.Reason)))
                .ForMember(dest => dest.RiskLevel, opt => opt.MapFrom(src => InferRiskLevel(src.Confidence)))
                .ForMember(dest => dest.HorizonDays, opt => opt.MapFrom(_ => 5))
                .ForMember(dest => dest.PredictedAt, opt => opt.MapFrom(src => src.RecommendedAtUtc));
        }

        private static string ExtractPattern(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return "UNKNOWN";
            }

            var chunks = value.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            foreach (var chunk in chunks)
            {
                if (chunk.StartsWith("Pattern=", StringComparison.OrdinalIgnoreCase))
                {
                    return chunk[8..].Trim();
                }
            }

            return "UNKNOWN";
        }

        private static string ExtractReason(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return "Aucune justification";
            }

            var chunks = value.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            foreach (var chunk in chunks)
            {
                if (chunk.StartsWith("Reason=", StringComparison.OrdinalIgnoreCase))
                {
                    return chunk[7..].Trim();
                }
            }

            return value;
        }

        private static string InferRiskLevel(decimal confidence)
        {
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
