using System.ComponentModel.DataAnnotations;
using BackPredictFinance.Common.enums;

namespace BackPredictFinance.ViewModels.ClientFinanceViewModels.Transactions
{
    public class TransactionCreateRequestViewModel
    {
        [Required]
        public string Symbol { get; set; } = string.Empty;

        [Required]
        public TransactionTypeEnum TransactionType { get; set; }

        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal Fees { get; set; }
        public DateTime? TimestampUtc { get; set; }

        [Required]
        public string PortfolioId { get; set; } = string.Empty;
    }
}
