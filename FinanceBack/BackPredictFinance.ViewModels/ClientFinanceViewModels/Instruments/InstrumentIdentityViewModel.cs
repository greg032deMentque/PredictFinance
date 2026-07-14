namespace BackPredictFinance.ViewModels.ClientFinanceViewModels.Instruments
{
    public sealed class InstrumentIdentityViewModel
    {
        public string InstrumentId { get; set; } = string.Empty;
        public string Symbol { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string AssetType { get; set; } = string.Empty;
        public string Exchange { get; set; } = string.Empty;
        public string Currency { get; set; } = string.Empty;
        public string? CountryCode { get; set; }
    }
}
