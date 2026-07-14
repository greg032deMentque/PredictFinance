using System;
using System.Collections.Generic;
using System.Text;

namespace BackPredictFinance.ViewModels.ClientFinanceViewModels.Simulation
{
    public class SimulationRequestViewModel
    {
        public string Symbol { get; set; } = string.Empty;
        public string Pattern { get; set; } = string.Empty;
        public decimal InvestmentAmount { get; set; }
        public int HorizonDays { get; set; }
    }
}
