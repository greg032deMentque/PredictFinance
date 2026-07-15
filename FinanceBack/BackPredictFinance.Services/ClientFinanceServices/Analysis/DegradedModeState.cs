using BackPredictFinance.Common.enums;

namespace BackPredictFinance.Services.ClientFinanceServices.Analysis
{
    public interface IDegradedModeState
    {
        FreshnessStatusEnum Status { get; }
        DateTime? CheckedAtUtc { get; }
        void MarkDegraded(DateTime snapshotTimestampUtc);
    }

    public sealed class DegradedModeState : IDegradedModeState
    {
        public FreshnessStatusEnum Status { get; private set; } = FreshnessStatusEnum.Fresh;
        public DateTime? CheckedAtUtc { get; private set; }

        public void MarkDegraded(DateTime snapshotTimestampUtc)
        {
            Status = FreshnessClassifier.Classify(snapshotTimestampUtc, DateTime.UtcNow);
            CheckedAtUtc = snapshotTimestampUtc;
        }
    }
}
