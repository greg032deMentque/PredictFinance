using BackPredictFinance.Common.enums;

namespace BackPredictFinance.ViewModels.ClientFinanceViewModels.Readings
{
    public sealed class SupportReadingSummaryViewModel
    {
        public SupportAvailabilityStatusEnum AvailabilityStatus { get; set; } = SupportAvailabilityStatusEnum.Unavailable;
        public string AvailabilityDisplayLabel { get; set; } = string.Empty;
        public PeaEligibilityStatusEnum PeaEligibilityStatus { get; set; } = PeaEligibilityStatusEnum.Unknown;
        public string PeaDisplayLabel { get; set; } = string.Empty;
    }
}
