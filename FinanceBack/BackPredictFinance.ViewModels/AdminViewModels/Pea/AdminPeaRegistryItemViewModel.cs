using BackPredictFinance.Common.enums;

namespace BackPredictFinance.ViewModels.AdminViewModels.Pea
{
    public sealed class AdminPeaRegistryItemViewModel
    {
        public string EntryId { get; set; } = string.Empty;
        public string AssetId { get; set; } = string.Empty;
        public string Symbol { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string UniverseId { get; set; } = string.Empty;
        public PeaEligibilityStatusEnum EligibilityStatus { get; set; }
        public PeaEligibilitySourceTypeEnum SourceType { get; set; }
        public string SourceReference { get; set; } = string.Empty;
        public DateTime? CheckedUtc { get; set; }
        public string PolicyVersion { get; set; } = string.Empty;
        public string ReviewerNote { get; set; } = string.Empty;
    }
}
