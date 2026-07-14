namespace BackPredictFinance.ViewModels.AdminViewModels.DataQuality
{
    public sealed class AdminDataQualityViewModel
    {
        public int AssetsMissingProfileSyncCount { get; set; }
        public int AssetsWithoutPeaRegistryCount { get; set; }
        public int PeaRegistryUnknownStatusCount { get; set; }
        public int CompletedAnalysisRunsWithoutModelSnapshotCount { get; set; }
        public int CompletedAnalysisRunsWithoutDecisionSignalCount { get; set; }
        public List<string> IssueSummaries { get; set; } = [];
    }
}
