using BackPredictFinance.Common.AnalysisV1;
using BackPredictFinance.Common.enums;

namespace BackPredictFinance.Services.ClientFinanceServices.Analysis
{
    /// <summary>
    /// Assemble la confiance expliquée (RM-27) à partir des sous-objets persistés du pattern principal.
    /// Le niveau de confiance est repris tel quel ; seuls les critères sont dérivés de l'état
    /// détection / validation / invalidation. Aucun recalcul du niveau.
    /// </summary>
    public interface IConfidenceBreakdownAssembler
    {
        /// <summary>
        /// Construit la décomposition de confiance d'un pattern principal persisté (vide si absent).
        /// </summary>
        ConfidenceBreakdown Build(PatternAssessmentContract? primaryPattern);
    }

    /// <summary>
    /// Implémente la dérivation déterministe des critères de confiance à partir d'un snapshot persisté.
    /// </summary>
    public sealed class ConfidenceBreakdownAssembler : IConfidenceBreakdownAssembler
    {
        private readonly IAnalysisAccompanimentWordingProvider _wordingProvider;

        public ConfidenceBreakdownAssembler(IAnalysisAccompanimentWordingProvider wordingProvider)
        {
            _wordingProvider = wordingProvider;
        }

        public ConfidenceBreakdown Build(PatternAssessmentContract? primaryPattern)
        {
            if (primaryPattern == null)
            {
                return new ConfidenceBreakdown();
            }

            return new ConfidenceBreakdown
            {
                Label = primaryPattern.Scoring.ConfidenceLabel ?? string.Empty,
                Criteria =
                [
                    BuildCriterion(
                        ConfidenceCriterionCodes.StructureCompatible,
                        CriterionSource.Detection,
                        primaryPattern.Detection.IsCompatible ? CriterionState.Met : CriterionState.Absent),
                    BuildCriterion(
                        ConfidenceCriterionCodes.PatternValidated,
                        CriterionSource.Validation,
                        MapValidationState(primaryPattern.Validation.State)),
                    BuildCriterion(
                        ConfidenceCriterionCodes.InvalidationNotTriggered,
                        CriterionSource.Invalidation,
                        MapInvalidationState(primaryPattern.Invalidation.State))
                ]
            };
        }

        private ConfidenceCriterion BuildCriterion(string code, CriterionSource source, CriterionState state)
        {
            return new ConfidenceCriterion
            {
                Code = code,
                Label = _wordingProvider.GetConfidenceCriterionLabel(code),
                State = state,
                Source = source
            };
        }

        // Toute valeur de statut inconnue ou intermédiaire (ex. "PENDING") retombe sur Partial plutôt
        // que Met/Absent : un critère de confiance ne doit jamais être présenté comme définitivement
        // acquis ou définitivement manquant sur un état qu'on ne reconnaît pas explicitement.
        private static CriterionState MapValidationState(string? state)
        {
            return (state ?? string.Empty).Trim().ToUpperInvariant() switch
            {
                "VALIDATED" => CriterionState.Met,
                "NOT_VALIDATED" => CriterionState.Absent,
                _ => CriterionState.Partial
            };
        }

        private static CriterionState MapInvalidationState(string? state)
        {
            return (state ?? string.Empty).Trim().ToUpperInvariant() switch
            {
                "ACTIVE" => CriterionState.Met,
                "INVALIDATED" => CriterionState.Absent,
                _ => CriterionState.Partial
            };
        }
    }
}
