namespace BackPredictFinance.ViewModels.ClientFinanceViewModels.Simulation
{
    public class SimulationRequestViewModel
    {
        public string Symbol { get; set; } = string.Empty;
        public string Pattern { get; set; } = string.Empty;
        public List<string> Patterns { get; set; } = [];
        public decimal InvestmentAmount { get; set; }
        public int HorizonDays { get; set; }
    }
}
