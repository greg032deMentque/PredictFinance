using BackPredictFinance.ViewModels.ClientFinanceViewModels.Instruments;

namespace BackPredictFinance.ViewModels.ClientFinanceViewModels.History
{
    public sealed class InstrumentHistoryViewModel
    {
        public InstrumentIdentityViewModel Instrument { get; set; } = new();
        public string Symbol { get; set; } = string.Empty;
        public List<HistoryItemViewModel> Items { get; set; } = [];
        public int ReturnedCount { get; set; }
    }
}
