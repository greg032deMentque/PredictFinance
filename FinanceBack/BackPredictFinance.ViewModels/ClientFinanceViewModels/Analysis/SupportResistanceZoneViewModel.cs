namespace BackPredictFinance.ViewModels.ClientFinanceViewModels.Analysis
{
    public sealed class SupportResistanceZoneViewModel
    {
        public decimal PriceLow { get; set; }
        public decimal PriceHigh { get; set; }
        public decimal PriceMid { get; set; }
        public int TouchCount { get; set; }
        public string ZoneType { get; set; } = string.Empty;
        public decimal Strength { get; set; }
    }
}
