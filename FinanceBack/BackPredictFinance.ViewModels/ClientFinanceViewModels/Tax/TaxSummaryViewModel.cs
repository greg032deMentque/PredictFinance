namespace BackPredictFinance.ViewModels.ClientFinanceViewModels.Tax
{
    public sealed class TaxSummaryViewModel
    {
        public int Year { get; set; }
        public string PortfolioId { get; set; } = string.Empty;
        public string PortfolioName { get; set; } = string.Empty;
        public string PortfolioTypeLabel { get; set; } = string.Empty;
        public decimal TaxRatePct { get; set; }
        public int? PeaAncienneteYears { get; set; }
        public decimal TotalRealizedPnl { get; set; }
        public decimal EstimatedTax { get; set; }
        public List<RealizedPositionViewModel> Positions { get; set; } = [];
    }
}
