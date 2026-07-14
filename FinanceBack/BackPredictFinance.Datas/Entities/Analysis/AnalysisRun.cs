using BackPredictFinance.Common.enums;

namespace BackPredictFinance.Datas.Entities
{
    public class AnalysisRun : AuditableEntityBase
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string UserId { get; set; } = string.Empty;
        public User User { get; set; } = null!;
        public string AssetId { get; set; } = string.Empty;
        public Asset Asset { get; set; } = null!;
        public AnalysisRunStatusEnum Status { get; set; } = AnalysisRunStatusEnum.Completed;
        public DateTime StartedAtUtc { get; set; }
        public DateTime? CompletedAtUtc { get; set; }
        public string RawPayload { get; set; } = "{}";
        public string? ErrorMessage { get; set; }

        public List<PatternAssessment> PatternAssessments { get; set; } = [];
        public DecisionSignal? DecisionSignal { get; set; }
        public ModelSnapshot? ModelSnapshot { get; set; }
    }
}
