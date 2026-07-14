using BackPredictFinance.Common.enums;
using BackPredictFinance.Common.AnalysisV1;

namespace BackPredictFinance.Services.ClientFinanceServices.Analysis
{
/// <summary>
/// Génère les explications pédagogiques et résumés textuels à partir de la vérité analytique.
/// </summary>
public interface IPedagogicalExplanationService
{
    /// <summary>
    /// Retourne la version de politique de rendu pédagogique.
    /// </summary>
    string PolicyVersion { get; }
    /// <summary>
    /// Construit l'explication détaillée d'un pattern projeté.
    /// </summary>
    PatternExplanation BuildPatternExplanation(PatternAssessmentContract patternAssessment, bool hasMultipleCompatiblePatterns, bool hasModelWarning);
    /// <summary>
    /// Construit le résumé pédagogique global d'une analyse.
    /// </summary>
    string BuildAnalysisSummary(AnalysisOutcome outcome, IReadOnlyList<PatternAssessmentContract> compatiblePatterns, AnalysisRecommendation? recommendation, PortfolioContext? portfolioContext);
}


    /// <summary>
    /// Implémente le rendu pédagogique des patterns et des analyses.
    /// </summary>
    public sealed class PedagogicalExplanationService : IPedagogicalExplanationService
    {
        public string PolicyVersion => "analysis-v1-explanation@prompt5";

        public PatternExplanation BuildPatternExplanation(PatternAssessmentContract patternAssessment, bool hasMultipleCompatiblePatterns, bool hasModelWarning)
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

        public string BuildAnalysisSummary(AnalysisOutcome outcome, IReadOnlyList<PatternAssessmentContract> compatiblePatterns, AnalysisRecommendation? recommendation, PortfolioContext? portfolioContext)
        {
            var holdingSentence = portfolioContext?.HoldsInstrument == true
                ? "Vous detenez deja cette valeur."
                : "Vous ne detenez pas actuellement cette valeur.";

            return outcome switch
            {
                AnalysisOutcome.NoCrediblePattern => $"{holdingSentence} Aucun pattern credible n'est retenu sur la fenetre analysee. La posture recommandee reste {FormatRecommendationKind(recommendation?.Kind)}.",
                AnalysisOutcome.MultipleCompatiblePatterns => BuildMultiplePatternSummary(compatiblePatterns, recommendation, holdingSentence),
                AnalysisOutcome.CrediblePatternFound => BuildSinglePatternSummary(compatiblePatterns.FirstOrDefault(), recommendation, holdingSentence),
                AnalysisOutcome.InsufficientData => $"{holdingSentence} Les donnees disponibles ne suffisent pas pour produire une analyse exploitable.",
                AnalysisOutcome.UnsupportedInstrument => "L'instrument demande ne fait pas partie du perimetre V1 actuellement pris en charge.",
                AnalysisOutcome.UnsupportedContext => "Le contexte portefeuille fourni ne permet pas encore de formuler une recommandation exploitable.",
                _ => $"{holdingSentence} L'analyse a ete produite avec une lecture prudente."
            };
        }

        private static string BuildWhyListed(PatternAssessmentContract patternAssessment)
        {
            if (!patternAssessment.Detection.IsCompatible)
            {
                return "Le pattern n'est pas retenu comme scenario compatible dans l'etat actuel du marche.";
            }

            return $"Le pattern est conserve car son etat {patternAssessment.Detection.CurrentPhaseLabel.ToLowerInvariant()} reste compatible avec les regles V1.";
        }

        private static string BuildPatternSummary(PatternAssessmentContract patternAssessment)
        {
            var confidenceLabel = string.IsNullOrWhiteSpace(patternAssessment.Scoring.ConfidenceLabel)
                ? "non classee"
                : patternAssessment.Scoring.ConfidenceLabel.ToLowerInvariant();

            return $"{patternAssessment.DisplayName} en phase {patternAssessment.Detection.CurrentPhaseLabel.ToLowerInvariant()} avec une confiance {confidenceLabel}. {patternAssessment.Detection.StatusReason}";
        }

        private static string BuildMultiplePatternSummary(IReadOnlyList<PatternAssessmentContract> compatiblePatterns, AnalysisRecommendation? recommendation, string holdingSentence)
        {
            var patternNames = compatiblePatterns
                .Select(x => x.DisplayName)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var listedPatterns = patternNames.Count == 0
                ? "plusieurs scenarios"
                : string.Join(", ", patternNames);

            return $"{holdingSentence} Plusieurs scenarios restent compatibles ({listedPatterns}). La recommandation {FormatRecommendationKind(recommendation?.Kind)} conserve une lecture prudente et ne remplace pas la coexistence des patterns.";
        }

        private static string BuildSinglePatternSummary(PatternAssessmentContract? patternAssessment, AnalysisRecommendation? recommendation, string holdingSentence)
        {
            if (patternAssessment == null)
            {
                return $"{holdingSentence} Un scenario credible existe mais son detail n'est pas disponible dans la reponse courante.";
            }

            return $"{holdingSentence} Le scenario principal retenu est {patternAssessment.DisplayName.ToLowerInvariant()}, actuellement {patternAssessment.Detection.CurrentPhaseLabel.ToLowerInvariant()}. La recommandation {FormatRecommendationKind(recommendation?.Kind)} reste alignee sur cette lecture.";
        }

        private static string FormatRecommendationKind(RecommendationKind? kind)
        {
            return kind switch
            {
                RecommendationKind.Monitor => "MONITOR",
                RecommendationKind.Buy => "BUY",
                RecommendationKind.Wait => "WAIT",
                RecommendationKind.Hold => "HOLD",
                RecommendationKind.Reinforce => "REINFORCE",
                RecommendationKind.Lighten => "LIGHTEN",
                RecommendationKind.Sell => "SELL",
                _ => "WAIT"
            };
        }
    }
}
