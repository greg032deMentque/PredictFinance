namespace BackPredictFinance.ViewModels.AdminViewModels.Instruments
{
    public sealed class AdminInstrumentDetailViewModel
    {
        public string AssetId { get; set; } = string.Empty;
        public string Symbol { get; set; } = string.Empty;
        public string ProviderSymbol { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Exchange { get; set; } = string.Empty;
        public string Currency { get; set; } = string.Empty;
        public string AssetType { get; set; } = string.Empty;
        public string? Country { get; set; }
        public string? Sector { get; set; }
        public string? Category { get; set; }
        public string? Summary { get; set; }
        public DateTime? LastProfileSyncUtc { get; set; }
        public List<string> ActiveUniverseIds { get; set; } = [];
        public int PeaRegistryEntriesCount { get; set; }
        public int QuoteSnapshotsCount { get; set; }
        public int CandleSnapshotsCount { get; set; }
        public int AnalysisRunsCount { get; set; }
    }
}
