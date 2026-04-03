using BackPredictFinance.Common.enums;

namespace BackPredictFinance.ViewModels.ClientFinanceViewModels
{
    public sealed class TradingPredictionCompatibilityViewModel
    {
        public string Symbol { get; set; } = string.Empty;
        public DateTime PredictedAt { get; set; }
        public TradingPatternEnum Pattern { get; set; } = TradingPatternEnum.DoubleTop;
        public string Phase { get; set; } = string.Empty;
        public decimal CurrentPrice { get; set; }
        public decimal LastProbability { get; set; }
        public decimal MeanProbability { get; set; }
        public decimal MaxProbability { get; set; }
        public decimal ProbabilityPct { get; set; }
        public decimal MeanProbabilityPct { get; set; }
        public decimal MaxProbabilityPct { get; set; }
        public int NWindows { get; set; }
        public decimal? TargetPrice { get; set; }
        public decimal? InvalidationPrice { get; set; }
        public decimal? NecklinePrice { get; set; }
        public List<TradingPredictionPatternCompatibilityViewModel> Patterns { get; set; } = [];
        public ModelStatusEnum ModelStatus { get; set; } = ModelStatusEnum.NoGo;
        public List<TradingPredictionModelCheckCompatibilityViewModel> ModelChecks { get; set; } = [];
        public string ModelMessage { get; set; } = string.Empty;
        public string ModelVersion { get; set; } = string.Empty;
        public decimal? Precision { get; set; }
        public decimal? F1 { get; set; }
        public decimal? RocAuc { get; set; }
        public int? PositiveSamples { get; set; }
        public decimal? SelectedThreshold { get; set; }
    }

    public sealed class TradingPredictionPatternCompatibilityViewModel
    {
        public TradingPatternEnum Pattern { get; set; } = TradingPatternEnum.DoubleTop;
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

    public sealed class TradingPredictionModelCheckCompatibilityViewModel
    {
        public ModelCheckEnum Check { get; set; }
        public ModelCheckStatusEnum Status { get; set; }
        public decimal? Value { get; set; }
        public decimal? Threshold { get; set; }
        public string Detail { get; set; } = string.Empty;
    }
}
