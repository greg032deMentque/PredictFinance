namespace BackPredictFinance.ViewModels.ClientFinanceViewModels.History
{
    public sealed class HistoryFeedViewModel
    {
        public List<HistoryItemViewModel> Items { get; set; } = [];
        public int ReturnedCount { get; set; }
    }
}
