namespace BackPredictFinance.ViewModels.ClientFinanceViewModels.Readings
{
    public sealed class ConfidenceBreakdownViewModel
    {
        // Niveau de confiance repris tel quel du snapshot (HIGH/MEDIUM/LOW/VERY_LOW).
        public string Level { get; set; } = string.Empty;
        public List<ConfidenceCriterionViewModel> Criteria { get; set; } = [];
    }
}
