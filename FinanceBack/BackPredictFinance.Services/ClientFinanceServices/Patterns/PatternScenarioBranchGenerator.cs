using BackPredictFinance.Common.AnalysisV1;
using BackPredictFinance.ViewModels.ClientFinanceViewModels.Patterns;

namespace BackPredictFinance.Services.ClientFinanceServices.Patterns
{
    public interface IPatternScenarioBranchGenerator
    {
        List<PatternScenarioBranchViewModel> Generate(PatternAssessmentContract assessment, bool holdsInstrument);
    }

    public sealed class PatternScenarioBranchGenerator : IPatternScenarioBranchGenerator
    {
        public List<PatternScenarioBranchViewModel> Generate(PatternAssessmentContract assessment, bool holdsInstrument)
        {
            ArgumentNullException.ThrowIfNull(assessment);

            var confirmationBranch = BuildConfirmationBranch(assessment, holdsInstrument);
            var invalidationBranch = BuildInvalidationBranch(assessment, holdsInstrument);

            return [confirmationBranch, invalidationBranch];
        }

        private PatternScenarioBranchViewModel BuildConfirmationBranch(PatternAssessmentContract assessment, bool holdsInstrument)
        {
            var necklineOrBreakout = assessment.Detection.StructuralPoints
                .FirstOrDefault(p => p.PointType.Contains("neckline", StringComparison.OrdinalIgnoreCase)
                    || p.PointType.Contains("breakout", StringComparison.OrdinalIgnoreCase));

            var triggerLevel = necklineOrBreakout?.Price;
            var isUpward = IsUpwardPattern(assessment);

            var confirmedPostureLabel = holdsInstrument ? "Conserver / Renforcer" : "Entrer en position";

            return new PatternScenarioBranchViewModel
            {
                TriggerLabel = BuildConfirmationTriggerLabel(assessment),
                TriggerLevel = triggerLevel,
                Direction = isUpward ? "Up" : "Down",
                ResultingState = "Confirmed",
                Posture = confirmedPostureLabel,
                Rationale = BuildConfirmationRationale(assessment, holdsInstrument)
            };
        }

        private PatternScenarioBranchViewModel BuildInvalidationBranch(PatternAssessmentContract assessment, bool holdsInstrument)
        {
            var invalidationLevel = assessment.Invalidation.InvalidationLevel;
            var isUpward = IsUpwardPattern(assessment);

            var invalidatedPostureLabel = holdsInstrument ? "Reduire / Sortir" : "Ne pas entrer";

            return new PatternScenarioBranchViewModel
            {
                TriggerLabel = BuildInvalidationTriggerLabel(assessment),
                TriggerLevel = invalidationLevel,
                Direction = isUpward ? "Down" : "Up",
                ResultingState = "Invalidated",
                Posture = invalidatedPostureLabel,
                Rationale = BuildInvalidationRationale(assessment, holdsInstrument)
            };
        }

        private static string BuildConfirmationTriggerLabel(PatternAssessmentContract assessment)
        {
            return IsUpwardPattern(assessment)
                ? "Franchissement de la neckline / niveau de cassure haussier"
                : "Cassure du support / niveau de confirmation baissier";
        }

        // La direction se lit sur l'identité de la figure (ex. bear_flag), pas sur le code de phase :
        // certains codes de phase (flag_structure_not_confirmed, flag_resistance_broken) ne portent pas la direction.
        private static bool IsUpwardPattern(PatternAssessmentContract assessment)
        {
            if (assessment.PatternId.Contains("bear", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            return !assessment.Detection.CurrentPhaseCode.Contains("bear", StringComparison.OrdinalIgnoreCase);
        }

        private static string BuildInvalidationTriggerLabel(PatternAssessmentContract assessment)
        {
            return string.IsNullOrWhiteSpace(assessment.Invalidation.InvalidationRuleCode)
                ? "Perte du niveau d invalidation"
                : $"Perte du niveau d invalidation ({assessment.Invalidation.InvalidationRuleCode})";
        }

        private static string BuildConfirmationRationale(PatternAssessmentContract assessment, bool holdsInstrument)
        {
            var patternLabel = string.IsNullOrWhiteSpace(assessment.DisplayName) ? assessment.PatternId : assessment.DisplayName;
            return holdsInstrument
                ? $"Si le {patternLabel} se confirme, la position detenue beneficie du mouvement directionnel prevu."
                : $"Si le {patternLabel} se confirme, une entree en position devient justifiee par le signal technique.";
        }

        private static string BuildInvalidationRationale(PatternAssessmentContract assessment, bool holdsInstrument)
        {
            var patternLabel = string.IsNullOrWhiteSpace(assessment.DisplayName) ? assessment.PatternId : assessment.DisplayName;
            return holdsInstrument
                ? $"Si le niveau d invalidation est rompu, la these du {patternLabel} est caduque : reduire ou sortir la position."
                : $"Si le niveau d invalidation est rompu, le {patternLabel} est invalide : ne pas entrer en position.";
        }
    }
}
