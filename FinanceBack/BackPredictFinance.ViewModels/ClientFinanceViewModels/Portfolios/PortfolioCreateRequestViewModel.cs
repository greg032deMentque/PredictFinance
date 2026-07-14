using System.ComponentModel.DataAnnotations;

namespace BackPredictFinance.ViewModels.ClientFinanceViewModels.Portfolios
{
    public sealed class PortfolioCreateRequestViewModel
    {
        [Required]
        [MaxLength(128)]
        public string Name { get; set; } = string.Empty;

        [Required]
        public string PortfolioType { get; set; } = string.Empty;
    }
}
