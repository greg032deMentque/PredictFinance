using System.ComponentModel.DataAnnotations;

namespace BackPredictFinance.ViewModels.ClientFinanceViewModels.History
{
    public sealed class InstrumentHistoryQueryViewModel
    {
        [Range(1, int.MaxValue)]
        public int Page { get; set; } = 1;

        [Range(1, 100)]
        public int PageSize { get; set; } = 20;

        public string SortDirection { get; set; } = "desc";
    }
}
