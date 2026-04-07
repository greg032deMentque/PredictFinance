using System;
using System.Collections.Generic;
using System.Text;

namespace BackPredictFinance.ViewModels.ClientFinanceViewModels
{
    public class AssetSearchItemViewModel
    {
        public string Symbol { get; set; } = string.Empty;
        public string AssetType { get; set; } = string.Empty;
        public string CompanyName { get; set; } = string.Empty;
        public string Market { get; set; } = string.Empty;
        public string Currency { get; set; } = "USD";
        public decimal LastPrice { get; set; }
        public decimal DayVariationPct { get; set; }
    }

}
