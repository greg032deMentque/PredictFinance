using BackPredictFinance.Common.AnalysisV1;
using BackPredictFinance.Common.enums;

namespace BackPredictFinance.Patterns.Common
{
    public sealed class ContinuationPatternAnalysisState
    {
        public string PhaseCode { get; set; } = string.Empty;
        public string PhaseLabel { get; set; } = string.Empty;
        public PatternStatus Status { get; set; }
        public bool IsCompatible { get; set; }
        public string StatusReason { get; set; } = string.Empty;
        public bool IsValidated { get; set; }
        public string ValidationReason { get; set; } = string.Empty;
        public string? ValidationRuleCode { get; set; }
        public bool IsInvalidated { get; set; }
        public string InvalidationReason { get; set; } = string.Empty;
        public string? InvalidationRuleCode { get; set; }
        public decimal Confidence { get; set; }
        public decimal CurrentPrice { get; set; }
        public decimal? ReferencePrice { get; set; }
        public decimal? TargetPrice { get; set; }
        public decimal? InvalidationPrice { get; set; }
        public bool IsPrimary { get; set; } = true;
        public List<PatternStructuralPoint> StructuralPoints { get; set; } = [];
        public List<string> ScoreReasons { get; set; } = [];
    }
}
