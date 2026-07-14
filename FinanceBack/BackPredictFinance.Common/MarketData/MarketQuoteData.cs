using BackPredictFinance.Common.enums;

namespace BackPredictFinance.Common.MarketData
{
    public sealed class MarketQuoteData
    {
        public string Symbol { get; set; } = string.Empty;
        public AssetTypeEnum AssetType { get; set; } = AssetTypeEnum.Stock;
        public decimal LastPrice { get; set; }
        public decimal DayVariationPct { get; set; }
        public DateTime AsOfUtc { get; set; }
    }
}
