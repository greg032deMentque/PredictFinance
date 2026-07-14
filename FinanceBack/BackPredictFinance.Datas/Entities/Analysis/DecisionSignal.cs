using BackPredictFinance.Common.enums;

namespace BackPredictFinance.Datas.Entities
{
    /// <summary>
    /// Conserve le signal de décision dérivé d'une analyse historisée.
    /// </summary>
    public class DecisionSignal : AuditableEntityBase
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string AnalysisRunId { get; set; } = string.Empty;
        public AnalysisRun AnalysisRun { get; set; } = null!;
        public RecommendationActionEnum Action { get; set; } = RecommendationActionEnum.Hold;
        public bool IsActionable { get; set; }
        public decimal Confidence { get; set; }
        public int HorizonDays { get; set; }
        public string Reason { get; set; } = string.Empty;
    }
}
