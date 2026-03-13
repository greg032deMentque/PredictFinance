using BackPredictFinance.Common.enums;

namespace BackPredictFinance.Services.PythonServices.Models
{
    public class PredictOut
    {
        public string Symbol { get; set; } = string.Empty;
        public DateTime PredictedAt { get; set; }
        public string Pattern { get; set; } = "DOUBLE_TOP";
        public string Phase { get; set; } = string.Empty;
        public decimal CurrentPrice { get; set; }
        public decimal LastProbability { get; set; }
        public decimal MeanProbability { get; set; }
        public decimal MaxProbability { get; set; }
        public decimal ProbabilityPct { get; set; }
        public decimal MeanProbabilityPct { get; set; }
        public decimal MaxProbabilityPct { get; set; }
        public int NWindows { get; set; }
        public string SuggestedAction { get; set; } = "hold";
        public bool IsActionable { get; set; }
        public decimal ActionConfidence { get; set; }
        public int HorizonDays { get; set; }
        public string ActionReason { get; set; } = string.Empty;
        public decimal? TargetPrice { get; set; }
        public decimal? InvalidationPrice { get; set; }
        public decimal? NecklinePrice { get; set; }
        public List<PatternPrediction> Patterns { get; set; } = [];
        public ModelStatusEnum ModelStatus { get; set; } = ModelStatusEnum.NoGo;
        public List<ModelCheckResult> ModelChecks { get; set; } = [];
        public string ModelMessage { get; set; } = string.Empty;
        public string ModelVersion { get; set; } = string.Empty;
        public decimal? Precision { get; set; }
        public decimal? F1 { get; set; }
        public decimal? RocAuc { get; set; }
        public int? PositiveSamples { get; set; }
        public decimal? SelectedThreshold { get; set; }
    }
}
