namespace BackPredictFinance.ViewModels.AdminViewModels.Instruments
{
    public sealed class AdminInstrumentRegistryItemViewModel
    {
        public string AssetId { get; set; } = string.Empty;
        public string Symbol { get; set; } = string.Empty;
        public string ProviderSymbol { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Exchange { get; set; } = string.Empty;
        public string Currency { get; set; } = string.Empty;
        public string AssetType { get; set; } = string.Empty;
        public string? Country { get; set; }
        public DateTime? LastProfileSyncUtc { get; set; }
        public List<string> ActiveUniverseIds { get; set; } = [];
        public bool HasConfirmedPeaEligibility { get; set; }
        public bool HasAnalysisHistory { get; set; }
    }
}
