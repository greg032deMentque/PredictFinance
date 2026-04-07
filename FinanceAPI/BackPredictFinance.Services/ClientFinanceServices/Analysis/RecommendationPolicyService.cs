using BackPredictFinance.Common.enums;
using BackPredictFinance.ViewModels.ClientFinanceViewModels.AnalysisV1;

namespace BackPredictFinance.Services.ClientFinanceServices.Analysis
{
    public sealed class RecommendationPolicyService : IRecommendationPolicyService
    {
        private readonly ITradingRecommendationService _tradingRecommendationService;

        public RecommendationPolicyService(ITradingRecommendationService tradingRecommendationService)
        {
            _tradingRecommendationService = tradingRecommendationService;
        }

        public Recommendation EvaluateAnalysis(AnalysisRequest request, IReadOnlyList<PatternAssessment> compatiblePatterns, AnalysisOutcome outcome)
        {
            ArgumentNullException.ThrowIfNull(request);
            ArgumentNullException.ThrowIfNull(compatiblePatterns);

            var holdingContext = request.PortfolioContext.HoldsInstrument ? "HELD" : "NOT_HELD";
            var basedOnPatternIds = compatiblePatterns
                .Select(x => x.PatternId)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (compatiblePatterns.Count == 0 || outcome == AnalysisOutcome.NoCrediblePattern)
            {
                return new Recommendation
                {
                    RecommendationId = Guid.NewGuid().ToString("N"),
                    Kind = RecommendationKind.Wait,
                    HoldingContext = holdingContext,
                    Rationale = request.PortfolioContext.HoldsInstrument
                        ? "Aucun pattern credible n'impose de modifier votre position a ce stade."
                        : "Aucun pattern credible ne justifie une prise de position immediate.",
                    BasedOnPatternIds = basedOnPatternIds,
                    PolicyVersion = "analysis-v1-policy@prompt3"
                };
            }

            var primaryPattern = compatiblePatterns
                .OrderByDescending(x => x.Trace.IsPrimaryDisplayCandidate)
                .ThenByDescending(x => x.Scoring.ConfidenceScore)
                .FirstOrDefault();

            if (primaryPattern == null)
            {
                return new Recommendation
                {
                    RecommendationId = Guid.NewGuid().ToString("N"),
                    Kind = RecommendationKind.Wait,
                    HoldingContext = holdingContext,
                    Rationale = "Aucun signal exploitable n'a ete conserve pour formuler une recommandation.",
                    BasedOnPatternIds = [],
                    PolicyVersion = "analysis-v1-policy@prompt3"
                };
            }

            var legacyResult = _tradingRecommendationService.EvaluateAnalysis(
                MapPattern(primaryPattern.PatternId),
                primaryPattern.Detection.CurrentPhaseCode,
                primaryPattern.Scoring.ConfidenceScore,
                primaryPattern.RiskHints.SuggestedTakeProfit,
                primaryPattern.Invalidation.InvalidationLevel);

            var kind = ResolveRecommendationKind(request.PortfolioContext.HoldsInstrument, primaryPattern, legacyResult.Action);

            return new Recommendation
            {
                RecommendationId = Guid.NewGuid().ToString("N"),
                Kind = kind,
                HoldingContext = holdingContext,
                Rationale = BuildRationale(kind, primaryPattern, request.PortfolioContext.HoldsInstrument),
                BasedOnPatternIds = basedOnPatternIds,
                ReviewHorizonDays = legacyResult.HorizonDays > 0 ? legacyResult.HorizonDays : null,
                PolicyVersion = "analysis-v1-policy@prompt3",
                WarningText = compatiblePatterns.Count > 1
                    ? "Plusieurs patterns restent compatibles; la recommandation conserve toutes les lectures compatibles et reste prudente."
                    : null
            };
        }

        private static TradingPatternEnum MapPattern(string? patternId)
        {
            return (patternId ?? string.Empty).Trim().ToUpperInvariant() switch
            {
                "HEAD_AND_SHOULDERS" => TradingPatternEnum.HeadAndShoulders,
                "DOUBLE_TOP" => TradingPatternEnum.DoubleTop,
                "DOUBLE_BOTTOM" => TradingPatternEnum.DoubleBottom,
                "CUP_AND_HANDLE" => TradingPatternEnum.CupAndHandle,
                "TRIANGLE" => TradingPatternEnum.Triangle,
                _ => TradingPatternEnum.DoubleTop
            };
        }

        private static RecommendationKind ResolveRecommendationKind(bool holdsInstrument, PatternAssessment primaryPattern, RecommendationActionEnum action)
        {
            if (!primaryPattern.Detection.IsCompatible)
            {
                return RecommendationKind.Wait;
            }

            if (holdsInstrument)
            {
                return action switch
                {
                    RecommendationActionEnum.Buy => RecommendationKind.Reinforce,
                    RecommendationActionEnum.Sell => RecommendationKind.Sell,
                    _ => primaryPattern.Detection.Status is PatternStatus.Invalidated or PatternStatus.Completed
                        ? RecommendationKind.Wait
                        : RecommendationKind.Hold
                };
            }

            return action switch
            {
                RecommendationActionEnum.Buy => RecommendationKind.Buy,
                RecommendationActionEnum.Sell => RecommendationKind.Monitor,
                _ => primaryPattern.Detection.Status is PatternStatus.Forming or PatternStatus.Monitoring or PatternStatus.Confirmed
                    ? RecommendationKind.Monitor
                    : RecommendationKind.Wait
            };
        }

        private static string BuildRationale(RecommendationKind kind, PatternAssessment patternAssessment, bool holdsInstrument)
        {
            var patternName = string.IsNullOrWhiteSpace(patternAssessment.DisplayName) ? "le scenario principal" : patternAssessment.DisplayName;
            var phaseLabel = string.IsNullOrWhiteSpace(patternAssessment.Detection.CurrentPhaseLabel) ? "en cours" : patternAssessment.Detection.CurrentPhaseLabel.ToLowerInvariant();
            var confidenceLabel = string.IsNullOrWhiteSpace(patternAssessment.Scoring.ConfidenceLabel) ? "non classee" : patternAssessment.Scoring.ConfidenceLabel.ToLowerInvariant();

            return kind switch
            {
                RecommendationKind.Buy => $"{patternName} reste {phaseLabel} avec une confiance {confidenceLabel}. La posture retenue est BUY pour une entree pedagogique.",
                RecommendationKind.Monitor => $"{patternName} reste {phaseLabel} avec une confiance {confidenceLabel}. La posture retenue est MONITOR en attendant un signal plus net.",
                RecommendationKind.Hold => $"{patternName} reste {phaseLabel} avec une confiance {confidenceLabel}. Comme vous etes deja expose, la posture retenue est HOLD.",
                RecommendationKind.Reinforce => $"{patternName} reste {phaseLabel} avec une confiance {confidenceLabel}. Comme vous detenez deja cette valeur, la posture retenue est REINFORCE.",
                RecommendationKind.Sell => $"{patternName} indique un scenario adverse {phaseLabel} avec une confiance {confidenceLabel}. Comme vous detenez deja cette valeur, la posture retenue est SELL.",
                RecommendationKind.Lighten => $"{patternName} conduit a une posture LIGHTEN pour reduire partiellement l'exposition actuelle.",
                _ => holdsInstrument
                    ? "Aucun changement de posture n'est recommande sur votre position actuelle."
                    : "Aucune prise de position immediate n'est recommandee."
            };
        }
    }
}
