using BackPredictFinance.Common.AnalysisV1;
using BackPredictFinance.Common.enums;
using BackPredictFinance.Patterns.Contracts;

namespace BackPredictFinance.Services.ClientFinanceServices.Analysis
{
    public interface IRiskEvaluationService
    {
        PatternRiskHints EvaluatePrimaryRisk(AnalysisExecutionArtifact executionArtifact, PatternAssessmentContract patternAssessment);
        AnalysisRiskContext BuildRiskContext(AnalysisExecutionArtifact executionArtifact);
        void ApplyVolumeConfidenceAdjustment(PatternScoring scoring, VolumeConfirmation volumeConfirmation);
    }

    public sealed class RiskEvaluationService : IRiskEvaluationService
    {
        /// <summary>
        /// Construit le plan de risque d'un pattern en combinant deux sources : les niveaux techniques
        /// du pattern lui-meme (invalidation/target) quand ils existent, et un plan de secours calcule
        /// a partir de l'ATR (Average True Range) qui reste disponible meme si le pattern n'a pas de
        /// niveaux explicites.
        /// </summary>
        public PatternRiskHints EvaluatePrimaryRisk(AnalysisExecutionArtifact executionArtifact, PatternAssessmentContract patternAssessment)
        {
            ArgumentNullException.ThrowIfNull(executionArtifact);
            ArgumentNullException.ThrowIfNull(patternAssessment);

            var executedPattern = executionArtifact.Patterns
                .FirstOrDefault(x => string.Equals(x.ContractAssessment.AssessmentId, patternAssessment.AssessmentId, StringComparison.Ordinal));

            var currentPrice = patternAssessment.Detection.CurrentPrice;
            var suggestedStopLoss = patternAssessment.Invalidation.InvalidationLevel;
            var suggestedTakeProfit = executedPattern?.TargetPrice;
            var riskRewardRatio = BuildRiskRewardRatio(currentPrice, suggestedStopLoss, suggestedTakeProfit);
            var hasRiskPlan = suggestedStopLoss.HasValue || suggestedTakeProfit.HasValue || riskRewardRatio.HasValue;

            // Le stop ATR n'est calcule que si le prix courant et l'ATR sont strictement positifs :
            // un ATR nul (historique trop court/plat) rendrait le stop indetermine ou aberrant.
            var atrValue = TechnicalIndicators.AverageTrueRange(executionArtifact.Candles);
            decimal? atrStopLoss = currentPrice > 0m && atrValue > 0m
                ? TechnicalIndicators.ComputeAtrBasedStopLoss(currentPrice, atrValue)
                : null;

            decimal? atrTarget1 = null;
            decimal? atrTarget2 = null;
            decimal? atrRrr = null;

            if (atrStopLoss.HasValue && currentPrice > 0m)
            {
                var (t1, t2) = TechnicalIndicators.ComputeAtrBasedTargets(currentPrice, atrStopLoss.Value);
                atrTarget1 = t1;
                atrTarget2 = t2;
                atrRrr = BuildRiskRewardRatio(currentPrice, atrStopLoss, atrTarget1);
            }

            var positionSizePct = atrStopLoss.HasValue && currentPrice > 0m
                ? TechnicalIndicators.ComputePositionSizePct(currentPrice, atrStopLoss.Value)
                : null;

            return new PatternRiskHints
            {
                HasRiskPlan = hasRiskPlan,
                SuggestedStopLoss = suggestedStopLoss,
                SuggestedTakeProfit = suggestedTakeProfit,
                RiskRewardRatio = riskRewardRatio,
                PositioningNote = BuildPositioningNote(patternAssessment, hasRiskPlan, suggestedStopLoss, suggestedTakeProfit),
                AtrStopLossPrice = atrStopLoss,
                AtrTarget1Price = atrTarget1,
                AtrTarget2Price = atrTarget2,
                AtrRiskRewardRatio = atrRrr,
                PositionSizePercent = positionSizePct
            };
        }

        /// <summary>
        /// Construit le contexte de risque global de l'analyse (indépendant du pattern retenu),
        /// utilisé pour l'affichage transverse (ATR, volume, position sizing). Contrairement à
        /// <see cref="EvaluatePrimaryRisk"/> qui combine niveaux techniques du pattern + repli ATR,
        /// ce contexte est calculé exclusivement à partir de l'ATR et du dernier close.
        /// </summary>
        public AnalysisRiskContext BuildRiskContext(AnalysisExecutionArtifact executionArtifact)
        {
            ArgumentNullException.ThrowIfNull(executionArtifact);

            var candles = executionArtifact.Candles;
            var atrValue = TechnicalIndicators.AverageTrueRange(candles);
            var currentPrice = candles.Count > 0 ? candles[^1].Close : 0m;

            decimal? stopLoss = currentPrice > 0m && atrValue > 0m
                ? TechnicalIndicators.ComputeAtrBasedStopLoss(currentPrice, atrValue)
                : null;

            decimal? target1 = null;
            decimal? target2 = null;
            decimal? rrr = null;

            if (stopLoss.HasValue && currentPrice > 0m)
            {
                var (t1, t2) = TechnicalIndicators.ComputeAtrBasedTargets(currentPrice, stopLoss.Value);
                target1 = t1;
                target2 = t2;
                rrr = BuildRiskRewardRatio(currentPrice, stopLoss, target1);
            }

            var (volumeRatio, volumeConfirmation) = TechnicalIndicators.ComputeVolumeConfirmation(candles);
            var volumeAvg20 = TechnicalIndicators.ComputeVolumeAvg20(candles);
            var positionSizePct = stopLoss.HasValue && currentPrice > 0m
                ? TechnicalIndicators.ComputePositionSizePct(currentPrice, stopLoss.Value)
                : null;

            return new AnalysisRiskContext
            {
                AtrValue = Math.Round(atrValue, 4),
                StopLossPrice = stopLoss.HasValue ? Math.Round(stopLoss.Value, 4) : null,
                Target1Price = target1.HasValue ? Math.Round(target1.Value, 4) : null,
                Target2Price = target2.HasValue ? Math.Round(target2.Value, 4) : null,
                RiskRewardRatio = rrr,
                VolumeAvg20 = Math.Round(volumeAvg20, 0),
                VolumeRatio = volumeRatio,
                VolumeConfirmation = volumeConfirmation,
                PositionSizePercent = positionSizePct
            };
        }

        /// <summary>
        /// Ajuste la confiance d'un pattern de ±5 points selon la confirmation de volume au breakout.
        /// Le volume est un signal indépendant de la géométrie du pattern : un breakout sur fort volume
        /// (&gt;1.5× la moyenne 20 jours) renforce la conviction, un volume faible (&lt;0.7×) l'affaiblit ;
        /// entre les deux (Neutral), aucun ajustement n'est appliqué.
        /// </summary>
        public void ApplyVolumeConfidenceAdjustment(PatternScoring scoring, VolumeConfirmation volumeConfirmation)
        {
            ArgumentNullException.ThrowIfNull(scoring);

            const decimal volumeBonus = 0.05m;
            const decimal volumeMalus = -0.05m;

            if (volumeConfirmation == VolumeConfirmation.Strong)
            {
                scoring.ConfidenceScore = Clamp01(scoring.ConfidenceScore + volumeBonus);
                scoring.ScoreReasons.Add("Volume de breakout fort (>1.5× moy20) : +5 points de confiance.");
            }
            else if (volumeConfirmation == VolumeConfirmation.Weak)
            {
                scoring.ConfidenceScore = Clamp01(scoring.ConfidenceScore + volumeMalus);
                scoring.ScoreReasons.Add("Volume de breakout faible (<0.7× moy20) : -5 points de confiance.");
            }

            scoring.ConfidenceLabel = ResolveConfidenceLabel(scoring.ConfidenceScore);
        }

        // Ratio reward/risk = distance au target / distance au stop, toutes deux en valeur absolue
        // pour rester valable quel que soit le sens (haussier/baissier). Retourne null (plutôt que 0
        // ou une valeur infinie) si le risque ou le gain calculé est nul — un ratio n'a alors pas de sens.
        private static decimal? BuildRiskRewardRatio(decimal currentPrice, decimal? stopLoss, decimal? takeProfit)
        {
            if (!stopLoss.HasValue || !takeProfit.HasValue)
            {
                return null;
            }

            var risk = Math.Abs(currentPrice - stopLoss.Value);
            if (risk <= 0m)
            {
                return null;
            }

            var reward = Math.Abs(takeProfit.Value - currentPrice);
            if (reward <= 0m)
            {
                return null;
            }

            return Math.Round(reward / risk, 4);
        }

        // Message pédagogique priorisé du cas le plus bloquant (pattern non compatible, scénario
        // invalidé) au plus favorable (plan complet), pour que l'utilisateur comprenne en un coup
        // d'œil pourquoi un plan de risque est absent ou partiel avant de voir le détail des niveaux.
        private static string BuildPositioningNote(PatternAssessmentContract patternAssessment, bool hasRiskPlan, decimal? stopLoss, decimal? takeProfit)
        {
            if (!patternAssessment.Detection.IsCompatible)
            {
                return "Aucun plan de risque n'est retenu car le pattern n'est pas compatible.";
            }

            if (patternAssessment.Invalidation.State == "INVALIDATED")
            {
                return "Le scenario est invalide a ce stade et ne justifie plus de plan de risque actif.";
            }

            if (hasRiskPlan && stopLoss.HasValue && takeProfit.HasValue)
            {
                return "Le plan de risque est derive des niveaux techniques detectes pour ce scenario.";
            }

            if (hasRiskPlan)
            {
                return "Le scenario reste suivi, mais le plan de risque est partiel car tous les niveaux techniques ne sont pas disponibles.";
            }

            return "Aucun plan de risque exploitable n'a pu etre derive de ce scenario.";
        }

        private static decimal Clamp01(decimal value)
        {
            if (value < 0m) return 0m;
            if (value > 1m) return 1m;
            return value;
        }

        // Seuils de label après ajustement volume : 0.80/0.60/0.35 délimitent HIGH/MEDIUM/LOW/VERY_LOW.
        // Ces bornes doivent rester cohérentes avec celles utilisées côté scoring initial du pattern
        // (avant application du bonus/malus volume) pour éviter un label incohérent entre les deux étapes.
        private static string ResolveConfidenceLabel(decimal confidence)
        {
            if (confidence >= 0.80m) return "HIGH";
            if (confidence >= 0.60m) return "MEDIUM";
            if (confidence >= 0.35m) return "LOW";
            return "VERY_LOW";
        }
    }
}
