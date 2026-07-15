using System.ComponentModel.DataAnnotations;

namespace BackPredictFinance.ViewModels.ClientFinanceViewModels.Screener
{
    public sealed class ScreenerPresetCreateViewModel
    {
        [Required]
        [MaxLength(128)]
        public string Name { get; set; } = string.Empty;

        [Required]
        public ScreenerQueryViewModel Query { get; set; } = new();
    }
}
