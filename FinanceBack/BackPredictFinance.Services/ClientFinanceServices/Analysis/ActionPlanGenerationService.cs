using System.Globalization;
using BackPredictFinance.Common.AnalysisV1;
using BackPredictFinance.Common.enums;

namespace BackPredictFinance.Services.ClientFinanceServices.Analysis
{
    /// <summary>
    /// Génère le plan d'action déterministe « Vos prochaines étapes » (RM-26).
    /// Reformule des vérités déjà calculées (niveaux de risque, horizon de revue, contexte de détention)
    /// sans introduire de nouveau chiffre : chaque valeur est une reprise formatée d'un champ source tracé.
    /// </summary>
    public interface IActionPlanGenerationService
    {
        /// <summary>
        /// Construit le plan d'action pour le contexte de détention réel du snapshot.
        /// En issue non-exécutable, seules les étapes d'attente / rappel de détention sont admissibles.
        /// </summary>
        ActionPlan Generate(
            AnalysisOutcome outcome,
            PatternAssessmentContract? primaryPattern,
            AnalysisRecommendation? recommendation,
            bool holdsInstrument,
            string currencyCode);
    }

    /// <summary>
    /// Implémente la génération déterministe du plan d'action à partir d'un snapshot persisté.
    /// </summary>
    public sealed class ActionPlanGenerationService : IActionPlanGenerationService
    {
        private const int MaxSteps = 3;
        private static readonly CultureInfo MoneyCulture = CultureInfo.GetCultureInfo("fr-FR");

        private readonly IAnalysisAccompanimentWordingProvider _wordingProvider;

        public ActionPlanGenerationService(IAnalysisAccompanimentWordingProvider wordingProvider)
        {
            _wordingProvider = wordingProvider;
        }

        public ActionPlan Generate(
            AnalysisOutcome outcome,
            PatternAssessmentContract? primaryPattern,
            AnalysisRecommendation? recommendation,
            bool holdsInstrument,
            string currencyCode)
        {
            var steps = IsExecutable(outcome) && primaryPattern != null
                ? BuildExecutableSteps(primaryPattern, recommendation, holdsInstrument, currencyCode)
                : BuildNonExecutableSteps(holdsInstrument);

            return new ActionPlan
            {
                PolicyVersion = _wordingProvider.PolicyVersion,
                Steps = steps.Take(MaxSteps).ToList()
            };
        }

        private static bool IsExecutable(AnalysisOutcome outcome)
            => outcome is AnalysisOutcome.CrediblePatternFound or AnalysisOutcome.MultipleCompatiblePatterns;

        private List<ActionPlanStep> BuildExecutableSteps(
            PatternAssessmentContract primaryPattern,
            AnalysisRecommendation? recommendation,
            bool holdsInstrument,
            string currencyCode)
        {
            var steps = new List<ActionPlanStep>();

            if (primaryPattern.Invalidation.InvalidationLevel.HasValue)
            {
                steps.Add(BuildStep(
                    ActionStepKind.NoteLevel,
                    "riskHints.invalidationPrice",
                    FormatMoney(primaryPattern.Invalidation.InvalidationLevel.Value, currencyCode)));
            }

            if (primaryPattern.RiskHints.SuggestedTakeProfit.HasValue)
            {
                steps.Add(BuildStep(
                    ActionStepKind.SetAlert,
                    "riskHints.targetPrice",
                    FormatMoney(primaryPattern.RiskHints.SuggestedTakeProfit.Value, currencyCode),
                    AlertTrigger.LevelCrossed));
            }

            if (recommendation?.ReviewHorizonDays is int horizonDays && horizonDays > 0)
            {
                steps.Add(BuildStep(
                    ActionStepKind.ReviewAt,
                    "recommendation.reviewHorizonDays",
                    $"{horizonDays} j"));
            }

            if (holdsInstrument)
            {
                steps.Add(BuildStep(
                    ActionStepKind.HoldingReminder,
                    "recommendation.holdingContext",
                    null));
            }

            if (steps.Count == 0)
            {
                steps.Add(BuildStep(ActionStepKind.WaitForData, "analysis.outcome", null));
            }

            return steps;
        }

        private List<ActionPlanStep> BuildNonExecutableSteps(bool holdsInstrument)
        {
            var steps = new List<ActionPlanStep>
            {
                BuildStep(ActionStepKind.WaitForData, "analysis.outcome", null)
            };

            if (holdsInstrument)
            {
                steps.Add(BuildStep(ActionStepKind.HoldingReminder, "recommendation.holdingContext", null));
            }

            return steps;
        }

        private ActionPlanStep BuildStep(ActionStepKind kind, string sourceField, string? value, AlertTrigger? alertTrigger = null)
        {
            return new ActionPlanStep
            {
                Kind = kind,
                Label = _wordingProvider.GetActionStepLabel(kind),
                SourceField = sourceField,
                Value = value,
                AlertTrigger = alertTrigger
            };
        }

        private static string FormatMoney(decimal value, string currencyCode)
        {
            var amount = value.ToString("N2", MoneyCulture);
            return string.IsNullOrWhiteSpace(currencyCode) ? amount : $"{amount} {currencyCode.Trim()}";
        }
    }
}
