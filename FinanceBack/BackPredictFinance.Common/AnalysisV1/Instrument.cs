namespace BackPredictFinance.Common.AnalysisV1
{
    public sealed class Instrument
    {
        public string InstrumentId { get; set; } = string.Empty;
        public string Symbol { get; set; } = string.Empty;
        public string ProviderSymbol { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string MarketCode { get; set; } = string.Empty;
        public string CountryCode { get; set; } = string.Empty;
        public string CurrencyCode { get; set; } = string.Empty;
        public string AssetType { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime? LastProfileSyncUtc { get; set; }
        public string? Summary { get; set; }
    }
}
