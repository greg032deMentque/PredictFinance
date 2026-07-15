namespace BackPredictFinance.ViewModels.ClientFinanceViewModels.Simulation
{
    public sealed class MultiSimulationResultViewModel
    {
        public string Symbol { get; set; } = string.Empty;
        public decimal InvestmentAmount { get; set; }
        public int HorizonDays { get; set; }
        public decimal CurrentPrice { get; set; }
        public string Currency { get; set; } = "EUR";
        public string GlobalMessage { get; set; } = string.Empty;
        public List<SimulationResultViewModel> PatternResults { get; set; } = [];
    }
}
