using BackPredictFinance.Common.enums;
using BackPredictFinance.ViewModels.ClientFinanceViewModels.Assets;

namespace BackPredictFinance.ViewModels.ClientFinanceViewModels.Instruments
{
    public sealed class InstrumentSummaryViewModel
    {
        public InstrumentIdentityViewModel Instrument { get; set; } = new();
        public string PerimeterLabel { get; set; } = string.Empty;
        public PeaEligibilityStatusEnum PeaEligibilityStatus { get; set; } = PeaEligibilityStatusEnum.Unknown;
        public string PeaDisplayLabel { get; set; } = string.Empty;
        public FreshnessViewModel Freshness { get; set; } = new();
        public bool HasPersistedAnalysis { get; set; }
        public string AnalysisAvailabilityLabel { get; set; } = string.Empty;
        public string? LatestAnalysisId { get; set; }
        public string? LatestSnapshotId { get; set; }
    }
}
