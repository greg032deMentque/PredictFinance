using BackPredictFinance.Common.enums;

namespace BackPredictFinance.Datas.Entities
{
    /// <summary>
    /// Référence canonique d'un instrument financier suivi par l'application.
    /// </summary>
    public class Asset : AuditableEntityBase
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Symbol { get; set; } = string.Empty;
        public string ProviderSymbol { get; set; } = string.Empty;
        public string? Name { get; set; }
        public string Exchange { get; set; } = string.Empty;
        public string Currency { get; set; } = "USD";
        public string? Country { get; set; }
        public string? Sector { get; set; }
        public string? Category { get; set; }
        public string? Summary { get; set; }
        public string? Isin { get; set; }
        public DateTime? LastProfileSyncUtc { get; set; }
        public AssetTypeEnum AssetType { get; set; }

        public List<UserAsset> UserAssets { get; set; } = [];
        public List<PriceHistory> PriceHistories { get; set; } = [];
        public List<AssetQuoteSnapshot> QuoteSnapshots { get; set; } = [];
        public List<AssetCandleSnapshot> CandleSnapshots { get; set; } = [];
        public List<AnalysisRun> AnalysisRuns { get; set; } = [];
        public List<AssetPeaEligibility> PeaEligibilities { get; set; } = [];
    }
}
