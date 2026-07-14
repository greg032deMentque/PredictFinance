namespace BackPredictFinance.Common.AnalysisV1
{
    public sealed class ActionPlan
    {
        public List<ActionPlanStep> Steps { get; set; } = [];
        public string PolicyVersion { get; set; } = string.Empty;
    }
}
