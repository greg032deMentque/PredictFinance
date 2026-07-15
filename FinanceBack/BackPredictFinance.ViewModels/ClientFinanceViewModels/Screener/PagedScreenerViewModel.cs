namespace BackPredictFinance.ViewModels.ClientFinanceViewModels.Screener
{
    public sealed class PagedScreenerViewModel
    {
        public List<ScreenerItemViewModel> Items { get; set; } = [];
        public int Total { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
    }
}
