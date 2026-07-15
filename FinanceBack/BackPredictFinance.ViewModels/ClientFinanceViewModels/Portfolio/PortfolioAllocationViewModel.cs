namespace BackPredictFinance.ViewModels.ClientFinanceViewModels.Portfolio
{
    public sealed class AllocationSliceViewModel
    {
        public string Label { get; set; } = string.Empty;
        public decimal WeightPct { get; set; }
        public decimal ValueEur { get; set; }
    }

    public sealed class ConcentrationAlertViewModel
    {
        public string Message { get; set; } = string.Empty;
    }

    public enum DiversificationRating
    {
        Concentrated,
        Moderate,
        Diversified
    }

    public sealed class PortfolioAllocationViewModel
    {
        public List<AllocationSliceViewModel> SectorAllocation { get; set; } = [];
        public List<AllocationSliceViewModel> CountryAllocation { get; set; } = [];
        public List<AllocationSliceViewModel> CurrencyAllocation { get; set; } = [];
        public decimal ConcentrationScore { get; set; }
        public DiversificationRating DiversificationRating { get; set; }
        public List<ConcentrationAlertViewModel> ConcentrationAlerts { get; set; } = [];
        public decimal? PortfolioReturn30d { get; set; }
        public decimal? PortfolioReturn90d { get; set; }
        public decimal? PortfolioReturn365d { get; set; }
        public decimal? BenchmarkReturn30d { get; set; }
        public decimal? BenchmarkReturn90d { get; set; }
        public decimal? BenchmarkReturn365d { get; set; }
        public bool BenchmarkUnavailable { get; set; }
    }
}
