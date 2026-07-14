using BackPredictFinance.Common.enums;

namespace BackPredictFinance.Common.MarketData
{
    public sealed class MarketAssetProfileData
    {
        public string Symbol { get; set; } = string.Empty;
        public string ProviderSymbol { get; set; } = string.Empty;
        public string CompanyName { get; set; } = string.Empty;
        public AssetTypeEnum AssetType { get; set; } = AssetTypeEnum.Stock;
        public string Exchange { get; set; } = string.Empty;
        public string Currency { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;
        public string Sector { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Summary { get; set; } = string.Empty;
        public decimal LastPrice { get; set; }
        public decimal DayVariationPct { get; set; }
        public DateTime AsOfUtc { get; set; }
    }
}
