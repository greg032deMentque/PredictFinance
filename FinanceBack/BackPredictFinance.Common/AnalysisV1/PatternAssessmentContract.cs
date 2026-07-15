using BackPredictFinance.Common.enums;

namespace BackPredictFinance.Common.AnalysisV1
{
    public sealed class PatternAssessmentContract
    {
        public string AssessmentId { get; set; } = string.Empty;
        public string PatternId { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string PedagogicalDescription { get; set; } = string.Empty;
        public PatternDirectionEnum Direction { get; set; } = PatternDirectionEnum.Unknown;
        public PatternAnalysisWindow AnalysisWindow { get; set; } = new();
        public PatternDetection Detection { get; set; } = new();
        public PatternValidation Validation { get; set; } = new();
        public PatternInvalidation Invalidation { get; set; } = new();
        public PatternScoring Scoring { get; set; } = new();
        public PatternRiskHints RiskHints { get; set; } = new();
        public PatternExplanation Explanation { get; set; } = new();
        public PatternTrace Trace { get; set; } = new();
    }
}
