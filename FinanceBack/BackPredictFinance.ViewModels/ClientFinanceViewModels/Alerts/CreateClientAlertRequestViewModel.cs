using BackPredictFinance.Common.enums;
using System.ComponentModel.DataAnnotations;

namespace BackPredictFinance.ViewModels.ClientFinanceViewModels.Alerts
{
    public sealed class CreateClientAlertRequestViewModel
    {
        [Required]
        public string Symbol { get; set; } = string.Empty;
        public AlertTrigger Trigger { get; set; }
        public decimal? LevelValue { get; set; }
        public string? PatternId { get; set; }
    }
}
