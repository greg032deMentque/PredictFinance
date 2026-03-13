using BackPredictFinance.Common.enums;

namespace BackPredictFinance.Datas.Entities
{
    public class PatternAssessment : AuditableEntityBase
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string AnalysisRunId { get; set; } = string.Empty;
        public AnalysisRun AnalysisRun { get; set; } = null!;
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
