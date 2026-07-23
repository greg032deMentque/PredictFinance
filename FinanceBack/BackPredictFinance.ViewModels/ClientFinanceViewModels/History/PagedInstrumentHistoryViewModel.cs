using BackPredictFinance.ViewModels.ClientFinanceViewModels.Instruments;
using BackPredictFinance.ViewModels.WebViewModels.PaginateViewModels;

namespace BackPredictFinance.ViewModels.ClientFinanceViewModels.History
{
    public sealed class PagedInstrumentHistoryViewModel : PagedResultViewModel<HistoryItemViewModel>
    {
        public InstrumentIdentityViewModel Instrument { get; set; } = new();
        public string Symbol { get; set; } = string.Empty;
    }
}
