using System;
using System.Collections.Generic;
using System.Text;

namespace BackPredictFinance.ViewModels.ClientFinanceViewModels
{
    public class TransactionCreateRequestViewModel
    {
        public string Symbol { get; set; } = string.Empty;
        public string TransactionType { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal Fees { get; set; }
        public DateTime? TimestampUtc { get; set; }
    }

}
