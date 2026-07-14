using BackPredictFinance.Common.enums;
using System.ComponentModel.DataAnnotations;

namespace BackPredictFinance.ViewModels.ClientFinanceViewModels.Patterns
{
    public sealed class PatternEvaluateRequestViewModel
    {
        [Required]
        public string Symbol { get; set; } = string.Empty;
        public HoldingStatusEnum? HoldingContext { get; set; }
    }
}
