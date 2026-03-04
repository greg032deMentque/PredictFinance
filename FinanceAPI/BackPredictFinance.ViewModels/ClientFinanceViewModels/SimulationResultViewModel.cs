using System;
using System.Collections.Generic;
using System.Text;

namespace BackPredictFinance.ViewModels.ClientFinanceViewModels
{


    public class SimulationResultViewModel
    {
        public string Symbol { get; set; } = string.Empty;
        public decimal InvestmentAmount { get; set; }
        public int HorizonDays { get; set; }
        public decimal EstimatedReturnAmount { get; set; }
        public decimal EstimatedReturnPct { get; set; }
        public decimal EstimatedFinalAmount { get; set; }
        public string Recommendation { get; set; } = string.Empty;
        public string Assumption { get; set; } = string.Empty;
    }
}
