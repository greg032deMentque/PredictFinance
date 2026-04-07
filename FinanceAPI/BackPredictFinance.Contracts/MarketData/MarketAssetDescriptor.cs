using BackPredictFinance.Common.enums;

namespace BackPredictFinance.Contracts.MarketData
{
    public sealed class MarketAssetDescriptor
    {
        public string Symbol { get; set; } = string.Empty;
        public string ProviderSymbol { get; set; } = string.Empty;
        public string CompanyName { get; set; } = string.Empty;
        public string Exchange { get; set; } = string.Empty;
        public string Currency { get; set; } = string.Empty;
        public AssetTypeEnum AssetType { get; set; } = AssetTypeEnum.Stock;
        public decimal LastPrice { get; set; }
        public decimal DayVariationPct { get; set; }
    }
}
