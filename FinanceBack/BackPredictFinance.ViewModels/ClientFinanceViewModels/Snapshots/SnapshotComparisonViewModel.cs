using BackPredictFinance.ViewModels.ClientFinanceViewModels.History;

namespace BackPredictFinance.ViewModels.ClientFinanceViewModels.Snapshots
{
    public sealed class SnapshotComparisonViewModel
    {
        public HistoryItemViewModel Left { get; set; } = new();
        public HistoryItemViewModel Right { get; set; } = new();
        public List<SnapshotDeltaItemViewModel> MarketChanges { get; set; } = [];
        public List<SnapshotDeltaItemViewModel> SupportChanges { get; set; } = [];
        public List<SnapshotDeltaItemViewModel> RecommendationChanges { get; set; } = [];
        public List<string> NonComparabilityReasons { get; set; } = [];
    }
}
