namespace BackPredictFinance.ViewModels.ClientFinanceViewModels.Portfolio
{
    public sealed class PortfolioViewModel
    {
        public List<PortfolioPositionViewModel> Positions { get; set; } = [];
        public decimal TotalInvestedAmount { get; set; }
        public decimal TotalOutstandingAmount { get; set; }
        public int OpenPositionCount { get; set; }
        public PortfolioAllocationViewModel? Allocation { get; set; }
    }
}
