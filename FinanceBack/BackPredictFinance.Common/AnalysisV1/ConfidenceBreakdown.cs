namespace BackPredictFinance.Common.AnalysisV1
{
    public sealed class ConfidenceBreakdown
    {
        // Libellé de confiance repris tel quel du scoring persisté (RM-27 : on explique, on ne recalcule pas).
        public string Label { get; set; } = string.Empty;
        public List<ConfidenceCriterion> Criteria { get; set; } = [];
    }
}
