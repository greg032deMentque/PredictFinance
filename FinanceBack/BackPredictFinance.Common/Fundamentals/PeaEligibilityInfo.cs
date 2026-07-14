using BackPredictFinance.Common.enums;

namespace BackPredictFinance.Common.Fundamentals
{
    public sealed class PeaEligibilityInfo
    {
        public PeaEligibilityStatusEnum Status { get; set; } = PeaEligibilityStatusEnum.Unknown;
        public PeaEligibilitySourceTypeEnum SourceType { get; set; } = PeaEligibilitySourceTypeEnum.Unknown;
        public string SourceReference { get; set; } = string.Empty;
        public DateTime? CheckedUtc { get; set; }
        public string PolicyVersion { get; set; } = string.Empty;
        public string ReviewerNote { get; set; } = string.Empty;
    }
}
