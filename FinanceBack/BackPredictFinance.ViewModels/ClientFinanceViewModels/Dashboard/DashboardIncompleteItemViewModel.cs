using BackPredictFinance.Common.enums;
using BackPredictFinance.ViewModels.ClientFinanceViewModels.Instruments;

namespace BackPredictFinance.ViewModels.ClientFinanceViewModels.Dashboard
{
    public sealed class DashboardIncompleteItemViewModel
    {
        public string AnalysisId { get; set; } = string.Empty;
        public InstrumentIdentityViewModel Instrument { get; set; } = new();
        public TechnicalAnalysisOutcomeTypeEnum Outcome { get; set; } = TechnicalAnalysisOutcomeTypeEnum.NoCrediblePattern;
        public string OutcomeDisplayLabel { get; set; } = string.Empty;
        public string ExplanationSummary { get; set; } = string.Empty;
    }
}
