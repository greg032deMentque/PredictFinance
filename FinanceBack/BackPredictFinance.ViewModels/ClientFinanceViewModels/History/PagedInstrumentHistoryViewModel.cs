using BackPredictFinance.ViewModels.ClientFinanceViewModels.Instruments;

namespace BackPredictFinance.ViewModels.ClientFinanceViewModels.History
{
    public sealed class PagedInstrumentHistoryViewModel
    {
        public InstrumentIdentityViewModel Instrument { get; set; } = new();
        public string Symbol { get; set; } = string.Empty;
        public List<HistoryItemViewModel> Items { get; set; } = [];
        public int Total { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
    }
}
