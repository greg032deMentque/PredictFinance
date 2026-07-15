namespace BackPredictFinance.Patterns.Common
{
    public sealed class SupportResistanceZone
    {
        public decimal PriceLow { get; set; }
        public decimal PriceHigh { get; set; }
        public decimal PriceMid { get; set; }
        public int TouchCount { get; set; }
        public string ZoneType { get; set; } = string.Empty;
        public decimal Strength { get; set; }
    }
}
