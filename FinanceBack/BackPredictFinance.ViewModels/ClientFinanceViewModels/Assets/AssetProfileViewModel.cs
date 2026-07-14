namespace BackPredictFinance.ViewModels.ClientFinanceViewModels.Assets
{
    public class AssetProfileViewModel
    {
        public string Symbol { get; set; } = string.Empty;
        public string ProviderSymbol { get; set; } = string.Empty;
        public string CompanyName { get; set; } = string.Empty;
        public string AssetType { get; set; } = string.Empty;
        public string Exchange { get; set; } = string.Empty;
        public string Currency { get; set; } = "USD";
        public string Country { get; set; } = string.Empty;
        public string Sector { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Summary { get; set; } = string.Empty;
        public decimal LastPrice { get; set; }
        public decimal DayVariationPct { get; set; }
        public DateTime? AsOfUtc { get; set; }
    }
}
