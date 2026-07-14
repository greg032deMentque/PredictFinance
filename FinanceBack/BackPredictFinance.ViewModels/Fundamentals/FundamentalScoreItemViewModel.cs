using AutoMapper;
using BackPredictFinance.Common.Fundamentals;
using BackPredictFinance.Common.enums;

namespace BackPredictFinance.ViewModels.Fundamentals
{
    public sealed class FundamentalScoreItemViewModel
    {
        public string Symbol { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public bool UsableScore { get; set; }
        public decimal? TotalScore { get; set; }
        public int CategoriesPresent { get; set; }
        public decimal CategoryCoverage { get; set; }
        public decimal? ProfitabilityScore { get; set; }
        public decimal? LiquidityScore { get; set; }
        public decimal? DebtScore { get; set; }
        public decimal? ValuationScore { get; set; }
        public decimal? DividendScore { get; set; }
        public List<string> MissingMetrics { get; set; } = [];
        public int? RankPosition { get; set; }
        public int? UniverseSize { get; set; }
        public List<string> Notes { get; set; } = [];
        public PeaEligibilityStatusEnum PeaEligibilityStatus { get; set; }
        public PeaEligibilitySourceTypeEnum PeaEligibilitySourceType { get; set; }
        public string PeaEligibilitySourceReference { get; set; } = string.Empty;
        public DateTime? PeaEligibilityCheckedUtc { get; set; }
        public string PeaEligibilityPolicyVersion { get; set; } = string.Empty;
        public string PeaEligibilityReviewerNote { get; set; } = string.Empty;
    }

    public sealed class FundamentalScoreItemViewModelProfile : Profile
    {
        public FundamentalScoreItemViewModelProfile()
        {
            CreateMap<FundamentalScoreResult, FundamentalScoreItemViewModel>()
                .ForMember(dest => dest.PeaEligibilityStatus, opt => opt.MapFrom(src => src.PeaEligibility.Status))
                .ForMember(dest => dest.PeaEligibilitySourceType, opt => opt.MapFrom(src => src.PeaEligibility.SourceType))
                .ForMember(dest => dest.PeaEligibilitySourceReference, opt => opt.MapFrom(src => src.PeaEligibility.SourceReference))
                .ForMember(dest => dest.PeaEligibilityCheckedUtc, opt => opt.MapFrom(src => src.PeaEligibility.CheckedUtc))
                .ForMember(dest => dest.PeaEligibilityPolicyVersion, opt => opt.MapFrom(src => src.PeaEligibility.PolicyVersion))
                .ForMember(dest => dest.PeaEligibilityReviewerNote, opt => opt.MapFrom(src => src.PeaEligibility.ReviewerNote));
        }
    }
}
