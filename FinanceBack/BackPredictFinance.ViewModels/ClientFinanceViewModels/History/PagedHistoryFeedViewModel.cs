namespace BackPredictFinance.ViewModels.ClientFinanceViewModels.History
{
    public sealed class PagedHistoryFeedViewModel
    {
        public List<HistoryItemViewModel> Items { get; set; } = [];
        public int Total { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
    }
}
