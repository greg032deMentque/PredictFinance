using BackPredictFinance.Common.enums;
using System.ComponentModel.DataAnnotations;

namespace BackPredictFinance.ViewModels.ClientFinanceViewModels.History
{
    public sealed class HistoryQueryViewModel
    {
        [Range(1, int.MaxValue)]
        public int Page { get; set; } = 1;

        [Range(1, 100)]
        public int PageSize { get; set; } = 20;

        public string? Symbol { get; set; }

        public RecommendationKind? Recommendation { get; set; }

        public string SortDirection { get; set; } = "desc";
    }
}
