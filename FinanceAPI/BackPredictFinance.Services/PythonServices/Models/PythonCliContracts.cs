using BackPredictFinance.Common.enums;

namespace BackPredictFinance.Services.PythonServices.Models
{
    public sealed class PythonPredictRequest
    {
        public string Symbol { get; set; } = string.Empty;
        public string ModelDir { get; set; } = string.Empty;
        public string Period { get; set; } = "6mo";
    }

    public sealed class PythonSimulationRequest
    {
        public string Symbol { get; set; } = string.Empty;
        public string Pattern { get; set; } = "DOUBLE_TOP";
        public string ModelDir { get; set; } = string.Empty;
        public string Period { get; set; } = "6mo";
        public decimal InvestmentAmount { get; set; }
        public int HorizonDays { get; set; }
        public decimal SellThreshold { get; set; }
        public decimal BuyThreshold { get; set; }
    }

    public sealed class PythonPredictPayload
    {
        public string Symbol { get; set; } = string.Empty;
        public DateTime AsOf { get; set; }
        public decimal MeanProbability { get; set; }
        public decimal MaxProbability { get; set; }
        public decimal LastProbability { get; set; }
        public int NWindows { get; set; }
    }

    public sealed class PythonSimulationPayload
    {
        public string Symbol { get; set; } = string.Empty;
        public string Pattern { get; set; } = "DOUBLE_TOP";
        public DateTime AsOf { get; set; }
        public decimal InvestmentAmount { get; set; }
        public int HorizonDays { get; set; }
        public decimal EstimatedReturnPct { get; set; }
        public decimal EstimatedReturnAmount { get; set; }
        public decimal EstimatedFinalAmount { get; set; }
        public string Recommendation { get; set; } = "hold";
        public decimal Confidence { get; set; }
        public string Assumption { get; set; } = string.Empty;
        public decimal LastProbability { get; set; }
        public decimal MeanProbability { get; set; }
        public decimal MaxProbability { get; set; }
        public int NWindows { get; set; }
    }

    public sealed class ModelCheckResult
    {
        public ModelCheckEnum Check { get; set; }
        public ModelCheckStatusEnum Status { get; set; }
        public decimal? Value { get; set; }
        public decimal? Threshold { get; set; }
        public string Detail { get; set; } = string.Empty;
    }

    public sealed class ModelQualityGate
    {
        public ModelStatusEnum ModelStatus { get; set; } = ModelStatusEnum.NoGo;
        public List<ModelCheckResult> ModelChecks { get; set; } = [];
        public string ModelMessage { get; set; } = string.Empty;
    }
}
