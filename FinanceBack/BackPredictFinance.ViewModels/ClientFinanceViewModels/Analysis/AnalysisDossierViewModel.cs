using BackPredictFinance.Common.AnalysisV1;

namespace BackPredictFinance.ViewModels.ClientFinanceViewModels.Analysis
{
    public sealed class AnalysisDossierViewModel
    {
        public string Id { get; set; } = string.Empty;
        public string Symbol { get; set; } = string.Empty;
        public string CompanyName { get; set; } = string.Empty;
        public string Outcome { get; set; } = string.Empty;
        public string OutcomeMessage { get; set; } = string.Empty;
        public string GlobalSummary { get; set; } = string.Empty;
        public DateTime PredictedAt { get; set; }
        public string ModelStatus { get; set; } = string.Empty;
        public string ModelMessage { get; set; } = string.Empty;
        public AnalysisWindowViewModel? AnalysisWindow { get; set; }
        public List<CandleViewModel> PriceSeries { get; set; } = [];
        public AnalysisPatternViewModel? MainPattern { get; set; }
        public List<AnalysisPatternViewModel> AlternativePatterns { get; set; } = [];
        public List<SupportResistanceZoneViewModel> SrZones { get; set; } = [];
        public AnalysisRiskContext? RiskContext { get; set; }
        public AnalysisTechnicalContext? TechnicalContext { get; set; }
    }
}
