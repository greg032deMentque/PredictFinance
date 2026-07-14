using BackPredictFinance.Common.enums;

namespace BackPredictFinance.Datas.Entities
{
    /// <summary>
    /// Conserve l'issue ex post d'un signal d'analyse historisé pour audit et KPI.
    /// </summary>
    public class SignalOutcome : AuditableEntityBase
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string AnalysisRunId { get; set; } = string.Empty;
        public AnalysisRun AnalysisRun { get; set; } = null!;
        public string PatternAssessmentId { get; set; } = string.Empty;
        public PatternAssessment PatternAssessment { get; set; } = null!;
        public string DecisionSignalId { get; set; } = string.Empty;
        public DecisionSignal DecisionSignal { get; set; } = null!;
        public SignalOutcomeEnum Outcome { get; set; } = SignalOutcomeEnum.StillOpen;
        public int EvaluationWindowDays { get; set; }
        public DateTime EvaluatedAtUtc { get; set; }
        public DateTime? FirstHitAtUtc { get; set; }
        public string PolicyVersion { get; set; } = string.Empty;
        public ConfidenceLabelEnum ConfidenceLabel { get; set; } = ConfidenceLabelEnum.Low;
    }
}
