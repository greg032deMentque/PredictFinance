using BackPredictFinance.Common.enums;

namespace BackPredictFinance.Common.AnalysisV1
{
    public sealed class ConfidenceCriterion
    {
        public string Code { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public CriterionState State { get; set; }
        public CriterionSource Source { get; set; }
    }
}
