using BackPredictFinance.Common.AnalysisV1;
using BackPredictFinance.Common.enums;
using BackPredictFinance.Common.Trading;
using static BackPredictFinance.Common.AnalysisV1.ConfidenceThresholds;

namespace BackPredictFinance.Services.ClientFinanceServices
{
    /// <summary>
    /// Convertit un signal technique en recommandation de trading structurée.
    /// </summary>
    public interface ITradingRecommendationService
    {
        /// <summary>
        /// Évalue une phase technique et ses niveaux associés pour produire une recommandation.
        /// </summary>
        TradingRecommendationResult EvaluateAnalysis(string phase, decimal confidenceScore, decimal? targetPrice, decimal? invalidationPrice);
    }

    /// <summary>
    /// Implémente la traduction d'un scénario technique en recommandation exploitable.
    /// </summary>
    public sealed class TradingRecommendationService : ITradingRecommendationService
    {
        private static readonly HashSet<string> BullishConfirmedPhases =
        [
            "bullish_breakout_confirmed",
            "double_bottom_breakout_confirmed",
            "inverse_hs_breakout_confirmed"
        ];

        private static readonly HashSet<string> BearishConfirmedPhases =
        [
            "bearish_breakout_confirmed",
            "double_top_breakout_confirmed",
            "hs_breakdown_confirmed"
        ];

        private static readonly HashSet<string> InvalidatedPhases =
        [
            "invalidated",
            "opposite_breakout_invalidated",
            "flag_support_broken",
            "flag_resistance_broken",
            "legacy_pattern_not_enabled"
        ];

        public TradingRecommendationResult EvaluateAnalysis(string phase, decimal confidenceScore, decimal? targetPrice, decimal? invalidationPrice)
        {
            var normalizedPhase = NormalizePhase(phase);
            var boundedConfidence = Clamp01(confidenceScore);

            if (BullishConfirmedPhases.Contains(normalizedPhase) && boundedConfidence >= 0.60m)
            {
                return new TradingRecommendationResult
                {
                    Action = RecommendationActionEnum.Buy,
                    IsActionable = true,
                    Confidence = boundedConfidence,
                    HorizonDays = 20,
                    RiskLevel = InferRiskLevel(boundedConfidence, true),
                    Reason = BuildDirectionalReason("haussier", normalizedPhase, RecommendationActionEnum.Buy, targetPrice, invalidationPrice)
                };
            }

            if (BearishConfirmedPhases.Contains(normalizedPhase) && boundedConfidence >= 0.60m)
            {
                return new TradingRecommendationResult
                {
                    Action = RecommendationActionEnum.Sell,
                    IsActionable = true,
                    Confidence = boundedConfidence,
                    HorizonDays = 20,
                    RiskLevel = InferRiskLevel(boundedConfidence, true),
                    Reason = BuildDirectionalReason("baissier", normalizedPhase, RecommendationActionEnum.Sell, targetPrice, invalidationPrice)
                };
            }

            if (InvalidatedPhases.Contains(normalizedPhase))
            {
                return new TradingRecommendationResult
                {
                    Action = RecommendationActionEnum.Hold,
                    IsActionable = false,
                    Confidence = boundedConfidence,
                    HorizonDays = 10,
                    RiskLevel = RiskLevelEnum.Information,
                    Reason = "Le scenario de continuation n'est pas exploitable dans sa forme actuelle. Aucune posture directionnelle n'est retenue."
                };
            }

            return new TradingRecommendationResult
            {
                Action = RecommendationActionEnum.Hold,
                IsActionable = false,
                Confidence = boundedConfidence,
                HorizonDays = 10,
                RiskLevel = RiskLevelEnum.Information,
                Reason = BuildHoldReason(normalizedPhase, boundedConfidence)
            };
        }

        private static string BuildDirectionalReason(string bias, string phase, RecommendationActionEnum action, decimal? targetPrice, decimal? invalidationPrice)
        {
            var targetPart = targetPrice.HasValue ? $" Objectif technique: {targetPrice.Value:0.####}." : string.Empty;
            var invalidationPart = invalidationPrice.HasValue ? $" Invalidation: {invalidationPrice.Value:0.####}." : string.Empty;
            return $"Pattern de continuation {bias} confirme ({phase}). La posture metier retenue est {action}.{targetPart}{invalidationPart}".Trim();
        }

        private static string BuildHoldReason(string phase, decimal confidence)
        {
            var safePhase = string.IsNullOrWhiteSpace(phase) ? "phase_inconnue" : phase;
            return $"Le pattern est encore en observation ({safePhase}) avec une confiance de {confidence:P0}. La posture metier retenue est Hold.";
        }

        private static RiskLevelEnum InferRiskLevel(decimal confidence, bool actionable)
        {
            if (!actionable)
            {
                return RiskLevelEnum.Information;
            }

            if (confidence >= HighFloor)
            {
                return RiskLevelEnum.Low;
            }

            if (confidence >= MediumFloor)
            {
                return RiskLevelEnum.Moderate;
            }

            return RiskLevelEnum.High;
        }

        private static string NormalizePhase(string phase)
        {
            return (phase ?? string.Empty).Trim().ToLowerInvariant();
        }

        private static decimal Clamp01(decimal value)
        {
            if (value < 0m)
            {
                return 0m;
            }

            if (value > 1m)
            {
                return 1m;
            }

            return value;
        }
    }
}
