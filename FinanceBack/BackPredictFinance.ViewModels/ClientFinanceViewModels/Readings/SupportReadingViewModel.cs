using BackPredictFinance.Common.enums;

namespace BackPredictFinance.ViewModels.ClientFinanceViewModels.Readings
{
    public sealed class SupportReadingViewModel
    {
        public SupportAvailabilityStatusEnum AvailabilityStatus { get; set; } = SupportAvailabilityStatusEnum.Unavailable;
        public string AvailabilityDisplayLabel { get; set; } = string.Empty;
        public string? ScoringVersion { get; set; }
        public string? ActiveUniverseId { get; set; }
        public PeaEligibilityStatusEnum PeaEligibilityStatus { get; set; } = PeaEligibilityStatusEnum.Unknown;
        public string PeaDisplayLabel { get; set; } = string.Empty;
        public decimal? CoverageRatio { get; set; }
        public decimal? CompositeScore { get; set; }
        public List<string> MissingCategorySummaries { get; set; } = [];
        public List<string> Notes { get; set; } = [];
    }
}
