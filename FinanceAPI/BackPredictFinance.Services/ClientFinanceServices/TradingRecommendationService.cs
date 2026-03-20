using BackPredictFinance.Common.enums;
using BackPredictFinance.Services.PythonServices.Models;

namespace BackPredictFinance.Services.ClientFinanceServices
{
    public interface ITradingRecommendationService
    {
        TradingRecommendationResult EvaluateAnalysis(PredictOut prediction);
        TradingRecommendationResult EvaluateSimulation(SimulationOut simulation);
    }

    public sealed class TradingRecommendationService : ITradingRecommendationService
    {
        private static readonly HashSet<string> ConfirmedBearishPhases =
        [
            "neckline_break_confirmed",
            "pullback_after_break"
        ];

        public TradingRecommendationResult EvaluateAnalysis(PredictOut prediction)
        {
            ArgumentNullException.ThrowIfNull(prediction);

            return Evaluate(
                prediction.Pattern,
                prediction.Phase,
                prediction.LastProbability,
                prediction.TargetPrice,
                prediction.InvalidationPrice);
        }

        public TradingRecommendationResult EvaluateSimulation(SimulationOut simulation)
        {
            ArgumentNullException.ThrowIfNull(simulation);

            return Evaluate(
                simulation.Pattern,
                simulation.Phase,
                simulation.LastProbability,
                simulation.TargetPrice,
                simulation.InvalidationPrice);
        }

        private static TradingRecommendationResult Evaluate(
            TradingPatternEnum pattern,
            string phase,
            decimal probability,
            decimal? targetPrice,
            decimal? invalidationPrice)
        {
            var normalizedPhase = NormalizePhase(phase);
            var boundedProbability = Clamp01(probability);

            if (pattern == TradingPatternEnum.DoubleTop &&
                ConfirmedBearishPhases.Contains(normalizedPhase) &&
                boundedProbability >= 0.60m)
            {
                return new TradingRecommendationResult
                {
                    Action = RecommendationActionEnum.Sell,
                    IsActionable = true,
                    Confidence = boundedProbability,
                    HorizonDays = 20,
                    RiskLevel = InferRiskLevel(boundedProbability, true),
                    Reason = BuildSellReason(normalizedPhase, targetPrice, invalidationPrice)
                };
            }

            if (pattern == TradingPatternEnum.DoubleTop && normalizedPhase == "invalidated")
            {
                return new TradingRecommendationResult
                {
                    Action = RecommendationActionEnum.Hold,
                    IsActionable = false,
                    Confidence = boundedProbability,
                    HorizonDays = 10,
                    RiskLevel = RiskLevelEnum.Information,
                    Reason = "Le scenario baissier est invalide. Aucune posture vendeuse n'est retenue."
                };
            }

            return new TradingRecommendationResult
            {
                Action = RecommendationActionEnum.Hold,
                IsActionable = false,
                Confidence = boundedProbability,
                HorizonDays = 10,
                RiskLevel = RiskLevelEnum.Information,
                Reason = BuildHoldReason(normalizedPhase, boundedProbability)
            };
        }

        private static string BuildSellReason(string phase, decimal? targetPrice, decimal? invalidationPrice)
        {
            var safePhase = string.IsNullOrWhiteSpace(phase) ? "phase_inconnue" : phase;
            var targetPart = targetPrice.HasValue ? $" Objectif technique: {targetPrice.Value:0.####}." : string.Empty;
            var invalidationPart = invalidationPrice.HasValue ? $" Invalidation: {invalidationPrice.Value:0.####}." : string.Empty;
            return $"Pattern baissier confirme ({safePhase}). La posture metier retenue est Sell.{targetPart}{invalidationPart}".Trim();
        }

        private static string BuildHoldReason(string phase, decimal probability)
        {
            var safePhase = string.IsNullOrWhiteSpace(phase) ? "phase_inconnue" : phase;
            return $"Le pattern est encore en observation ({safePhase}) avec une probabilite de {probability:P0}. La posture metier retenue est Hold.";
        }

        private static RiskLevelEnum InferRiskLevel(decimal confidence, bool actionable)
        {
            if (!actionable)
            {
                return RiskLevelEnum.Information;
            }

            if (confidence >= 0.75m)
            {
                return RiskLevelEnum.Low;
            }

            if (confidence >= 0.45m)
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

    public sealed class TradingRecommendationResult
    {
        public RecommendationActionEnum Action { get; set; } = RecommendationActionEnum.Hold;
        public bool IsActionable { get; set; }
        public decimal Confidence { get; set; }
        public int HorizonDays { get; set; }
        public string Reason { get; set; } = string.Empty;
        public RiskLevelEnum RiskLevel { get; set; } = RiskLevelEnum.Information;
    }
}
