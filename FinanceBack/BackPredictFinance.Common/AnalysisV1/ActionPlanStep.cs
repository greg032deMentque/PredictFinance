using BackPredictFinance.Common.enums;

namespace BackPredictFinance.Common.AnalysisV1
{
    public sealed class ActionPlanStep
    {
        public ActionStepKind Kind { get; set; }
        public string Label { get; set; } = string.Empty;
        // Champ d'analyse dont l'étape découle (token d'audit, ex. "riskHints.invalidationPrice").
        public string SourceField { get; set; } = string.Empty;
        // Valeur reprise telle quelle de la source, jamais recalculée (RM-26).
        public string? Value { get; set; }
        public AlertTrigger? AlertTrigger { get; set; }
    }
}
