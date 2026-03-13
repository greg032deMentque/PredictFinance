using BackPredictFinance.Common.enums;

namespace BackPredictFinance.Services.PythonServices.Models
{
    public sealed class PythonPredictRequest
    {
        public string Symbol { get; set; } = string.Empty;
        public string Pattern { get; set; } = "DOUBLE_TOP";
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

    public sealed class PythonPatternAssessmentPayload
    {
        public string Pattern { get; set; } = string.Empty;
        public string Phase { get; set; } = string.Empty;
        public decimal Probability { get; set; }
        public decimal Confidence { get; set; }
        public decimal CurrentPrice { get; set; }
        public decimal? NecklinePrice { get; set; }
        public decimal? TargetPrice { get; set; }
        public decimal? InvalidationPrice { get; set; }
        public DateTime? FirstPeakAtUtc { get; set; }
        public DateTime? SecondPeakAtUtc { get; set; }
        public bool IsPrimary { get; set; }
    }

    public sealed class PythonDecisionSignalPayload
    {
        public string Action { get; set; } = "hold";
        public bool Actionable { get; set; }
        public decimal Confidence { get; set; }
        public string Reason { get; set; } = string.Empty;
        public int HorizonDays { get; set; }
    }

    public sealed class PythonPredictPayload
    {
        public string Symbol { get; set; } = string.Empty;
        public string Pattern { get; set; } = "DOUBLE_TOP";
        public string Phase { get; set; } = string.Empty;
        public DateTime AsOf { get; set; }
        public decimal CurrentPrice { get; set; }
        public decimal MeanProbability { get; set; }
        public decimal MaxProbability { get; set; }
        public decimal LastProbability { get; set; }
        public int NWindows { get; set; }
        public List<PythonPatternAssessmentPayload> PatternAssessments { get; set; } = [];
        public PythonDecisionSignalPayload DecisionSignal { get; set; } = new();
    }

    public sealed class PythonSimulationPayload
    {
        public string Symbol { get; set; } = string.Empty;
        public string Pattern { get; set; } = "DOUBLE_TOP";
        public string Phase { get; set; } = string.Empty;
        public DateTime AsOf { get; set; }
        public decimal CurrentPrice { get; set; }
        public decimal? TargetPrice { get; set; }
        public decimal? InvalidationPrice { get; set; }
        public decimal InvestmentAmount { get; set; }
        public int HorizonDays { get; set; }
        public decimal EstimatedReturnPct { get; set; }
        public decimal EstimatedReturnAmount { get; set; }
        public decimal EstimatedFinalAmount { get; set; }
        public string Recommendation { get; set; } = "hold";
        public bool Actionable { get; set; }
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
        public ModelMetricsSummary Metrics { get; set; } = new();
    }
}
