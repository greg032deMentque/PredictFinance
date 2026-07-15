using BackPredictFinance.Common.enums;
using BackPredictFinance.ViewModels.ClientFinanceViewModels.Analysis;

namespace BackPredictFinance.ViewModels.ClientFinanceViewModels.Simulation
{
    public class SimulationResultViewModel
    {
        public string Symbol { get; set; } = string.Empty;
        public string Pattern { get; set; } = string.Empty;
        public string Phase { get; set; } = string.Empty;
        public decimal InvestmentAmount { get; set; }
        public int HorizonDays { get; set; }
        public decimal EstimatedReturnAmount { get; set; }
        public decimal EstimatedReturnPct { get; set; }
        public decimal EstimatedFinalAmount { get; set; }
        public string Assumption { get; set; } = string.Empty;
        public decimal CurrentPrice { get; set; }
        public string Currency { get; set; } = "EUR";
        public decimal Probability { get; set; }
        public RecommendationActionEnum RecommendationAction { get; set; } = RecommendationActionEnum.Hold;
        public string RecommendationReason { get; set; } = string.Empty;
        public RiskLevelEnum RiskLevel { get; set; } = RiskLevelEnum.Information;
        public bool IsActionable { get; set; }
        public decimal? TargetPrice { get; set; }
        public decimal? InvalidationPrice { get; set; }
        public List<SimulationScenarioViewModel> Scenarios { get; set; } = [];
        public List<CandleViewModel> PriceSeries { get; set; } = [];
        public List<StructuralPointViewModel> StructuralPoints { get; set; } = [];
    }
}
