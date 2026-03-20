using System;
using System.Collections.Generic;
using System.Text;

using BackPredictFinance.Common.enums;

namespace BackPredictFinance.ViewModels.ClientFinanceViewModels
{
    public class SimulationResultViewModel
    {
        public string Symbol { get; set; } = string.Empty;
        public TradingPatternEnum Pattern { get; set; } = TradingPatternEnum.DoubleTop;
        public string Phase { get; set; } = string.Empty;
        public decimal InvestmentAmount { get; set; }
        public int HorizonDays { get; set; }
        public decimal EstimatedReturnAmount { get; set; }
        public decimal EstimatedReturnPct { get; set; }
        public decimal EstimatedFinalAmount { get; set; }
        public string Assumption { get; set; } = string.Empty;
        public decimal CurrentPrice { get; set; }
        public decimal Probability { get; set; }
        public RecommendationActionEnum RecommendationAction { get; set; } = RecommendationActionEnum.Hold;
        public string RecommendationReason { get; set; } = string.Empty;
        public RiskLevelEnum RiskLevel { get; set; } = RiskLevelEnum.Information;
        public bool IsActionable { get; set; }
        public decimal? TargetPrice { get; set; }
        public decimal? InvalidationPrice { get; set; }
    }
}
