namespace BackPredictFinance.ViewModels.ClientFinanceViewModels.Simulation
{
    public sealed class SimulationScenarioViewModel
    {
        public string Label { get; set; } = string.Empty;
        public decimal? TargetPrice { get; set; }
        public decimal EstimatedReturnPct { get; set; }
        public decimal EstimatedReturnAmount { get; set; }
        public decimal EstimatedFinalAmount { get; set; }
        public decimal? Probability { get; set; }
    }
}
