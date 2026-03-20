using BackPredictFinance.Common.enums;

namespace BackPredictFinance.Services.PythonServices.Models
{
    public sealed class PatternPrediction
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
}
