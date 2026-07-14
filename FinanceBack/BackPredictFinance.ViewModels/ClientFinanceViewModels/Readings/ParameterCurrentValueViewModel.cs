namespace BackPredictFinance.ViewModels.ClientFinanceViewModels.Readings
{
    public sealed class ParameterCurrentValueViewModel
    {
        public bool IsAvailable { get; set; }
        public decimal? NumericValue { get; set; }
        public string DisplayValue { get; set; } = string.Empty;
        public string AvailabilityLabel { get; set; } = string.Empty;
        public string SourceLabel { get; set; } = string.Empty;
        public DateTime? AsOfUtc { get; set; }
    }
}
