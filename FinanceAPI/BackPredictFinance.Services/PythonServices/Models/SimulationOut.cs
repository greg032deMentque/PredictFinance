using BackPredictFinance.Common.enums;

namespace BackPredictFinance.Services.PythonServices.Models
{
    public sealed class SimulationOut
    {
        public string Symbol { get; set; } = string.Empty;
        public TradingPatternEnum Pattern { get; set; } = TradingPatternEnum.DoubleTop;
        public string Phase { get; set; } = string.Empty;
        public DateTime SimulatedAt { get; set; }
        public decimal CurrentPrice { get; set; }
        public decimal? TargetPrice { get; set; }
        public decimal? InvalidationPrice { get; set; }
        public decimal InvestmentAmount { get; set; }
        public int HorizonDays { get; set; }
        public decimal EstimatedReturnPct { get; set; }
        public decimal EstimatedReturnAmount { get; set; }
        public decimal EstimatedFinalAmount { get; set; }
        public string Assumption { get; set; } = string.Empty;
        public decimal LastProbability { get; set; }
        public decimal MeanProbability { get; set; }
        public decimal MaxProbability { get; set; }
        public int NWindows { get; set; }
    }
}
