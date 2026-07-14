using AutoMapper;
using BackPredictFinance.Common.Fundamentals;

namespace BackPredictFinance.ViewModels.Fundamentals
{
    public sealed class FundamentalScoreResponseViewModel
    {
        public string UniverseId { get; set; } = string.Empty;
        public string ScoringVersion { get; set; } = string.Empty;
        public string EligibilityPolicyVersion { get; set; } = string.Empty;
        public string ProviderId { get; set; } = string.Empty;
        public DateTime AsOfUtc { get; set; }
        public string AsOfUtcSemantics { get; set; } = string.Empty;
        public string? DataSnapshotId { get; set; }
        public List<FundamentalScoreItemViewModel> Results { get; set; } = [];
    }

    public sealed class FundamentalScoreResponseViewModelProfile : Profile
    {
        public FundamentalScoreResponseViewModelProfile()
        {
            CreateMap<FundamentalScoreResponse, FundamentalScoreResponseViewModel>();
        }
    }
}
