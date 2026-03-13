using BackPredictFinance.Common.enums;

namespace BackPredictFinance.Datas.Entities
{
    public class AnalysisBatch : AuditableEntityBase
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string UserId { get; set; } = string.Empty;
        public User User { get; set; } = null!;
        public TradingPatternEnum RequestedPattern { get; set; } = TradingPatternEnum.DoubleTop;
        public string Status { get; set; } = "Completed";
        public int ItemCount { get; set; }
        public DateTime RequestedAtUtc { get; set; }
        public DateTime? CompletedAtUtc { get; set; }
        public string? ErrorMessage { get; set; }
        public List<AnalysisRun> Runs { get; set; } = [];
    }
}
