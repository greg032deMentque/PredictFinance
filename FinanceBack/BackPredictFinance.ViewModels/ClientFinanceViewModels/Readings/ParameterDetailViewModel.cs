using BackPredictFinance.Common.enums;
using BackPredictFinance.ViewModels.ClientFinanceViewModels.Instruments;

namespace BackPredictFinance.ViewModels.ClientFinanceViewModels.Readings
{
    public sealed class ParameterDetailViewModel
    {
        public string ParameterId { get; set; } = string.Empty;
        public string CategoryCode { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public string RoleInCategory { get; set; } = string.Empty;
        public string SimpleDefinition { get; set; } = string.Empty;
        public string HowToReadCurrentValue { get; set; } = string.Empty;
        public string WhyItMatters { get; set; } = string.Empty;
        public string LimitsOfInterpretation { get; set; } = string.Empty;
        public string WhatItSupports { get; set; } = string.Empty;
        public string WhatItDoesNotProve { get; set; } = string.Empty;
        public string ImplicationWithoutPosition { get; set; } = string.Empty;
        public string ImplicationWithPosition { get; set; } = string.Empty;
        public InstrumentIdentityViewModel Instrument { get; set; } = new();
        public HoldingStatusEnum HoldingStatus { get; set; } = HoldingStatusEnum.NotHeld;
        public string HoldingContextLabel { get; set; } = string.Empty;
        public ParameterCurrentValueViewModel CurrentValue { get; set; } = new();
        public PeaEligibilityStatusEnum PeaEligibilityStatus { get; set; } = PeaEligibilityStatusEnum.Unknown;
        public string PeaDisplayLabel { get; set; } = string.Empty;
    }
}
