using BackPredictFinance.Common.enums;

namespace BackPredictFinance.Common.AnalysisV1
{
    public sealed class PatternDetection
    {
        public bool IsCompatible { get; set; }
        public PatternStatus Status { get; set; }
        public string CurrentPhaseCode { get; set; } = string.Empty;
        public string CurrentPhaseLabel { get; set; } = string.Empty;
        public string StatusReason { get; set; } = string.Empty;
        public decimal CurrentPrice { get; set; }
        public List<PatternStructuralPoint> StructuralPoints { get; set; } = [];
    }
}
