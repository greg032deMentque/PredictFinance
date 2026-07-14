using BackPredictFinance.Common.enums;
using BackPredictFinance.ViewModels.ClientFinanceViewModels.Instruments;
using BackPredictFinance.ViewModels.ClientFinanceViewModels.Readings;

namespace BackPredictFinance.ViewModels.ClientFinanceViewModels.Analysis
{
    public sealed class AnalysisDetailViewModel
    {
        public string AnalysisId { get; set; } = string.Empty;
        public InstrumentIdentityViewModel Instrument { get; set; } = new();
        public DateTime GeneratedAtUtc { get; set; }
        public TechnicalAnalysisOutcomeTypeEnum Outcome { get; set; } = TechnicalAnalysisOutcomeTypeEnum.NoCrediblePattern;
        public string OutcomeDisplayLabel { get; set; } = string.Empty;
        public MarketReadingSummaryViewModel MarketReading { get; set; } = new();
        public ConfidenceBreakdownViewModel ConfidenceBreakdown { get; set; } = new();
        public SupportReadingSummaryViewModel SupportReading { get; set; } = new();
        public RecommendationSummaryViewModel Recommendation { get; set; } = new();
        public ActionPlanViewModel ActionPlan { get; set; } = new();
        public string WhyRecommendation { get; set; } = string.Empty;
        public string PedagogicalSummary { get; set; } = string.Empty;
        public string SnapshotId { get; set; } = string.Empty;
        public string HistoryRoute { get; set; } = string.Empty;
        public string CompactSummary { get; set; } = string.Empty;
        public string ModelMessage { get; set; } = string.Empty;
        public ExPostEvaluationViewModel ExPostEvaluation { get; set; } = ExPostEvaluationViewModel.NotApplicable();
    }
}
