using System;
using System.Collections.Generic;
using System.Text;

namespace BackPredictFinance.ViewModels.ClientFinanceViewModels
{
    public class SimulationRequestViewModel
    {
        public string Symbol { get; set; } = string.Empty;
        public string Pattern { get; set; } = "DOUBLE_TOP";
        public decimal InvestmentAmount { get; set; }
        public int HorizonDays { get; set; }
    }
}
