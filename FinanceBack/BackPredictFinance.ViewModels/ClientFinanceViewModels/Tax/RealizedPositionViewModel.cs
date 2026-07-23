namespace BackPredictFinance.ViewModels.ClientFinanceViewModels.Tax
{
    public sealed class RealizedPositionViewModel
    {
        public string Symbol { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public decimal RealizedPnl { get; set; }
        public List<RealizedSaleViewModel> Sales { get; set; } = [];
        public bool HasDataIntegrityWarning { get; set; }
    }
}
