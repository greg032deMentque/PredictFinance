using BackPredictFinance.Common.enums;
using BackPredictFinance.Contracts.Analysis;

namespace BackPredictFinance.Services.ClientFinanceServices.Analysis
{
    public interface IPedagogicalExplanationService
    {
        string PolicyVersion { get; }
        PatternExplanation BuildPatternExplanation(PatternAssessment patternAssessment, bool hasMultipleCompatiblePatterns, bool hasModelWarning);
        string BuildAnalysisSummary(AnalysisOutcomeEnum outcome, IReadOnlyList<PatternAssessment> compatiblePatterns, AnalysisRecommendation? recommendation, PortfolioContext? portfolioContext);
    }

    public sealed class PedagogicalExplanationService : IPedagogicalExplanationService
    {
        public string PolicyVersion => "analysis-v1-explanation@prompt8";

        public PatternExplanation BuildPatternExplanation(PatternAssessment patternAssessment, bool hasMultipleCompatiblePatterns, bool hasModelWarning)
        {
            ArgumentNullException.ThrowIfNull(patternAssessment);

            return new PatternExplanation
            {
                WhyListed = BuildWhyListed(patternAssessment),
                PedagogicalSummary = BuildPatternSummary(patternAssessment),
                AmbiguityNote = hasMultipleCompatiblePatterns
                    ? "Plusieurs patterns restent compatibles au meme instant, ce qui impose une lecture prudente."
                    : null,
                LimitationsNote = hasModelWarning
                    ? "Le moteur d'analyse API-owned signale une limitation de qualite qui impose une lecture prudente."
                    : null
            };
        }

        public string BuildAnalysisSummary(AnalysisOutcomeEnum outcome, IReadOnlyList<PatternAssessment> compatiblePatterns, AnalysisRecommendation? recommendation, PortfolioContext? portfolioContext)
        {
            var holdingSentence = portfolioContext?.HoldsInstrument == true
                ? "Vous detenez deja cette valeur."
                : "Vous ne detenez pas actuellement cette valeur.";

            return outcome switch
            {
                AnalysisOutcomeEnum.NoCrediblePattern => $"{holdingSentence} Aucun pattern credible n'est retenu sur la fenetre analysee. La posture recommandee reste {FormatRecommendationAction(recommendation?.RecommendationAction)}.",
                AnalysisOutcomeEnum.MultipleCompatiblePatterns => BuildMultiplePatternSummary(compatiblePatterns, recommendation, holdingSentence),
                AnalysisOutcomeEnum.CrediblePatternFound => BuildSinglePatternSummary(compatiblePatterns.FirstOrDefault(), recommendation, holdingSentence),
                AnalysisOutcomeEnum.InsufficientData => $"{holdingSentence} Les donnees disponibles ne suffisent pas pour produire une analyse exploitable.",
                AnalysisOutcomeEnum.UnsupportedInstrument => "L'instrument demande ne fait pas partie du perimetre V1 actuellement pris en charge.",
                AnalysisOutcomeEnum.UnsupportedContext => "Le contexte portefeuille fourni ne permet pas encore de formuler une recommandation exploitable.",
                _ => $"{holdingSentence} L'analyse a ete produite avec une lecture prudente."
            };
        }

        private static string BuildWhyListed(PatternAssessment patternAssessment)
        {
            if (!patternAssessment.Detection.IsCompatible)
            {
                return "Le pattern n'est pas retenu comme scenario compatible dans l'etat actuel du marche.";
            }

            return $"Le pattern est conserve car son etat {patternAssessment.Detection.CurrentPhaseLabel.ToLowerInvariant()} reste compatible avec les regles V1.";
        }

        private static string BuildPatternSummary(PatternAssessment patternAssessment)
        {
            var confidenceLabel = string.IsNullOrWhiteSpace(patternAssessment.Scoring.ConfidenceLabel)
                ? "non classee"
                : patternAssessment.Scoring.ConfidenceLabel.ToLowerInvariant();

            return $"{patternAssessment.DisplayName} en phase {patternAssessment.Detection.CurrentPhaseLabel.ToLowerInvariant()} avec une confiance {confidenceLabel}. {patternAssessment.Detection.StatusReason}";
        }

        private static string BuildMultiplePatternSummary(IReadOnlyList<PatternAssessment> compatiblePatterns, AnalysisRecommendation? recommendation, string holdingSentence)
        {
            var patternNames = compatiblePatterns
                .Select(x => x.DisplayName)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var listedPatterns = patternNames.Count == 0
                ? "plusieurs scenarios"
                : string.Join(", ", patternNames);

            return $"{holdingSentence} Plusieurs scenarios restent compatibles ({listedPatterns}). La recommandation {FormatRecommendationAction(recommendation?.RecommendationAction)} conserve une lecture prudente et ne remplace pas la coexistence des patterns.";
        }

        private static string BuildSinglePatternSummary(PatternAssessment? patternAssessment, AnalysisRecommendation? recommendation, string holdingSentence)
        {
            if (patternAssessment == null)
            {
                return $"{holdingSentence} Un scenario credible existe mais son detail n'est pas disponible dans la reponse courante.";
            }

            return $"{holdingSentence} Le scenario principal retenu est {patternAssessment.DisplayName.ToLowerInvariant()}, actuellement {patternAssessment.Detection.CurrentPhaseLabel.ToLowerInvariant()}. La recommandation {FormatRecommendationAction(recommendation?.RecommendationAction)} reste alignee sur cette lecture.";
        }

        private static string FormatRecommendationAction(RecommendationActionEnum? action)
        {
            return action switch
            {
                RecommendationActionEnum.Buy => "BUY",
                RecommendationActionEnum.Sell => "SELL",
                RecommendationActionEnum.Hold => "HOLD",
                RecommendationActionEnum.Reinforce => "REINFORCE",
                RecommendationActionEnum.Lighten => "LIGHTEN",
                RecommendationActionEnum.Monitor => "MONITOR",
                RecommendationActionEnum.Wait => "WAIT",
                _ => "WAIT"
            };
        }
    }
}
