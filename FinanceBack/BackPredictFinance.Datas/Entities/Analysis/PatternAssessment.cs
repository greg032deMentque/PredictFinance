using BackPredictFinance.Common.enums;

namespace BackPredictFinance.Datas.Entities
{
    /// <summary>
    /// Conserve l'évaluation d'un pattern détecté dans une exécution d'analyse.
    /// </summary>
    public class PatternAssessment : AuditableEntityBase
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string AnalysisRunId { get; set; } = string.Empty;
        public AnalysisRun AnalysisRun { get; set; } = null!;
        public string PatternId { get; set; } = string.Empty;
        public string Phase { get; set; } = string.Empty;
        public PatternProgressStatusEnum ProgressStatus { get; set; } = PatternProgressStatusEnum.Forming;
        public PatternDirectionEnum Direction { get; set; } = PatternDirectionEnum.Unknown;
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
