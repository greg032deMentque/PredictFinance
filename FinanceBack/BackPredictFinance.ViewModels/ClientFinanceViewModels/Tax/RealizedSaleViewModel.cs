namespace BackPredictFinance.ViewModels.ClientFinanceViewModels.Tax
{
    public sealed class RealizedSaleViewModel
    {
        public DateTime SaleDate { get; set; }
        public decimal Quantity { get; set; }
        public decimal SellPrice { get; set; }
        public decimal AvgCostAtSale { get; set; }
        public decimal RealizedPnl { get; set; }
    }
}
