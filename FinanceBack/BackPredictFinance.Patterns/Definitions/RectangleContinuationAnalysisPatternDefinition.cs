using BackPredictFinance.Common.AnalysisV1;
using BackPredictFinance.Common.enums;
using BackPredictFinance.Common.MarketData;
using BackPredictFinance.Patterns.Abstractions;
using BackPredictFinance.Patterns.Common;

namespace BackPredictFinance.Patterns.Definitions
{
    public sealed class RectangleContinuationAnalysisPatternDefinition : ContinuationPatternDefinitionBase
    {
        public RectangleContinuationAnalysisPatternDefinition(IPatternMarketDataProvider marketDataProvider)
            : base(marketDataProvider)
        {
        }

        public override string PatternId => PatternIds.RectangleContinuation;
        public override string ModelVersion => "analysis-v1-rectangle-continuation@lot3-atr";
        public override int HistoryLookbackMonths => 6;
        protected override int MinimumRequiredCandles => 44;
        protected override string DisplayName => "Rectangle continuation";
        protected override string PedagogicalDescription => "Consolidation laterale apres tendance, directionnelle seulement apres breakout confirme dans le sens de la tendance prealable.";

        protected override ContinuationPatternAnalysisState Analyze(AnalysisRequest request, IReadOnlyList<TickerCandle> candles)
        {
            if (candles.Count < MinimumRequiredCandles)
            {
                return new ContinuationPatternAnalysisState
                {
                    PhaseCode = "insufficient_history",
                    PhaseLabel = "Historique insuffisant",
                    Status = PatternStatus.Forming,
                    IsCompatible = false,
                    StatusReason = "Le moteur n'a pas recu assez de bougies pour evaluer ce pattern de maniere deterministe.",
                    ValidationReason = "Une profondeur historique minimale est requise avant toute validation.",
                    InvalidationReason = "Aucune invalidation n'est interpretable avec un historique insuffisant.",
                    Confidence = 0m,
                    CurrentPrice = candles[^1].Close,
                    ScoreReasons = ["Le nombre minimal de bougies n'est pas atteint pour ce pattern."]
                };
            }
            var rangeWindow = PatternTechnicals.Tail(candles, 24);
            var priorWindow = candles.Take(Math.Max(candles.Count - rangeWindow.Count, 0)).TakeLast(20).ToList();
            var currentPrice = candles[^1].Close;
            var support = rangeWindow.Min(candle => candle.Low);
            var resistance = rangeWindow.Max(candle => candle.High);
            var rangeHeight = resistance - support;
            var averageClose = PatternTechnicals.AverageClose(rangeWindow);
            var atr = PatternTechnicals.VolatilityUnit(rangeWindow, currentPrice);
            var tolerance = PatternThresholds.RectangleTouchToleranceAtrMultiple * atr;
            var touchesResistance = rangeWindow.Count(candle => Math.Abs(candle.High - resistance) <= tolerance);
            var touchesSupport = rangeWindow.Count(candle => Math.Abs(candle.Low - support) <= tolerance);
            var slopeHigh = PatternTechnicals.ComputeSlope(rangeWindow.Select(candle => candle.High).ToList());
            var slopeLow = PatternTechnicals.ComputeSlope(rangeWindow.Select(candle => candle.Low).ToList());
            var priorTrend = ResolveDirectionalTrend(priorWindow);
            var isFlatEnough = averageClose > 0m
                && Math.Abs(slopeHigh) <= PatternThresholds.RectangleMaxBoundarySlopeAtrPerCandle * atr
                && Math.Abs(slopeLow) <= PatternThresholds.RectangleMaxBoundarySlopeAtrPerCandle * atr;
            var hasStructure = rangeHeight > 0m
                && averageClose > 0m
                && rangeHeight / averageClose >= PatternThresholds.RectangleMinHeightRatio
                && rangeHeight / averageClose <= PatternThresholds.RectangleMaxHeightRatio
                && touchesResistance >= PatternThresholds.RectangleMinTouchesPerBoundary
                && touchesSupport >= PatternThresholds.RectangleMinTouchesPerBoundary
                && isFlatEnough;
            var breakoutUp = currentPrice > resistance + (PatternThresholds.BreakoutAtrMultiple * atr);
            var breakoutDown = currentPrice < support - (PatternThresholds.BreakoutAtrMultiple * atr);
            var targetUp = resistance + rangeHeight;
            var targetDown = support - rangeHeight;
            var confidence = BuildConfidence(hasStructure, priorTrend, breakoutUp, breakoutDown, touchesResistance, touchesSupport);

            if (!hasStructure)
            {
                return new ContinuationPatternAnalysisState
                {
                    PhaseCode = "structure_not_confirmed",
                    PhaseLabel = "Structure de rectangle insuffisante",
                    Status = PatternStatus.Forming,
                    IsCompatible = false,
                    StatusReason = "La congestion laterale avec bornes quasi horizontales n'est pas suffisamment etablie.",
                    ValidationReason = "Le breakout ne peut pas etre valide tant que le rectangle n'est pas materialise.",
                    InvalidationReason = "Aucune invalidation directionnelle exploitable tant que la structure n'est pas lisible.",
                    Confidence = confidence,
                    CurrentPrice = currentPrice,
                    ReferencePrice = resistance,
                    InvalidationPrice = support,
                    StructuralPoints = PatternTechnicals.BuildBoundaryPoints(rangeWindow[^1].Date, ("support", support), ("resistance", resistance)),
                    ScoreReasons = ["Le rectangle exige des bornes repetees et peu pentees.", "Le contexte reste insuffisant pour une lecture de continuation."]
                };
            }

            if (priorTrend == DirectionalTrend.None)
            {
                return new ContinuationPatternAnalysisState
                {
                    PhaseCode = "neutral_rectangle_without_prior_trend",
                    PhaseLabel = "Rectangle neutre sans tendance prealable",
                    Status = PatternStatus.Monitoring,
                    IsCompatible = false,
                    StatusReason = "La structure ressemble a un rectangle, mais sans tendance prealable significative la lecture stricte de continuation reste invalide.",
                    ValidationReason = "Un breakout hors range serait necessaire mais le contexte de continuation n'est pas etabli.",
                    InvalidationReason = "Le cas reste bilateral faute de tendance prealable significative.",
                    Confidence = confidence,
                    CurrentPrice = currentPrice,
                    ReferencePrice = resistance,
                    InvalidationPrice = support,
                    StructuralPoints = PatternTechnicals.BuildBoundaryPoints(rangeWindow[^1].Date, ("support", support), ("resistance", resistance)),
                    ScoreReasons = ["Le rectangle peut exister structurellement.", "Le contexte de continuation est insuffisant sans tendance prealable significative."]
                };
            }

            if (priorTrend == DirectionalTrend.Up && breakoutUp)
            {
                return new ContinuationPatternAnalysisState
                {
                    PhaseCode = "bullish_breakout_confirmed",
                    PhaseLabel = "Breakout haussier confirme",
                    Status = PatternStatus.Confirmed,
                    IsCompatible = true,
                    StatusReason = "Le prix cloture au-dessus de la resistance du rectangle dans le sens de la tendance prealable.",
                    IsValidated = true,
                    ValidationReason = "Le breakout valide la lecture de continuation haussiere.",
                    ValidationRuleCode = "RECTANGLE_UPSIDE_BREAKOUT_CLOSE",
                    InvalidationReason = "Le scenario reste actif tant que le prix ne reintegre pas durablement le rectangle.",
                    Confidence = Math.Max(confidence, 0.72m),
                    CurrentPrice = currentPrice,
                    ReferencePrice = resistance,
                    TargetPrice = targetUp,
                    InvalidationPrice = resistance,
                    StructuralPoints = PatternTechnicals.BuildBoundaryPoints(rangeWindow[^1].Date, ("support", support), ("resistance", resistance), ("breakout", currentPrice)),
                    ScoreReasons = ["Le breakout sort du range.", "La direction du breakout respecte la tendance haussiere prealable.", "La cible pedagogique projette la hauteur du rectangle au-dessus de la resistance."]
                };
            }

            if (priorTrend == DirectionalTrend.Down && breakoutDown)
            {
                return new ContinuationPatternAnalysisState
                {
                    PhaseCode = "bearish_breakout_confirmed",
                    PhaseLabel = "Breakout baissier confirme",
                    Status = PatternStatus.Confirmed,
                    IsCompatible = true,
                    StatusReason = "Le prix cloture sous le support du rectangle dans le sens de la tendance baissiere prealable.",
                    IsValidated = true,
                    ValidationReason = "Le breakout valide la lecture de continuation baissiere.",
                    ValidationRuleCode = "RECTANGLE_DOWNSIDE_BREAKOUT_CLOSE",
                    InvalidationReason = "Le scenario reste actif tant que le prix ne reintegre pas durablement la zone laterale.",
                    Confidence = Math.Max(confidence, 0.72m),
                    CurrentPrice = currentPrice,
                    ReferencePrice = support,
                    TargetPrice = targetDown,
                    InvalidationPrice = support,
                    StructuralPoints = PatternTechnicals.BuildBoundaryPoints(rangeWindow[^1].Date, ("support", support), ("resistance", resistance), ("breakout", currentPrice)),
                    ScoreReasons = ["Le breakout sort du range.", "La direction du breakout respecte la tendance baissiere prealable.", "La cible pedagogique projette la hauteur du rectangle sous le support."]
                };
            }

            if ((priorTrend == DirectionalTrend.Up && breakoutDown) || (priorTrend == DirectionalTrend.Down && breakoutUp))
            {
                return new ContinuationPatternAnalysisState
                {
                    PhaseCode = "opposite_breakout_invalidated",
                    PhaseLabel = "Breakout oppose a la continuation",
                    Status = PatternStatus.Invalidated,
                    IsCompatible = false,
                    StatusReason = "Le breakout detecte part contre la direction attendue de continuation.",
                    ValidationReason = "Le breakout ne valide pas la these de continuation attendue.",
                    IsInvalidated = true,
                    InvalidationReason = "Le breakout oppose invalide la lecture stricte de continuation.",
                    InvalidationRuleCode = "RECTANGLE_OPPOSITE_BREAKOUT",
                    Confidence = 0.20m,
                    CurrentPrice = currentPrice,
                    ReferencePrice = priorTrend == DirectionalTrend.Up ? support : resistance,
                    InvalidationPrice = priorTrend == DirectionalTrend.Up ? support : resistance,
                    StructuralPoints = PatternTechnicals.BuildBoundaryPoints(rangeWindow[^1].Date, ("support", support), ("resistance", resistance), ("breakout", currentPrice)),
                    ScoreReasons = ["Le rectangle existe mais le breakout part a l'oppose de la these de continuation."]
                };
            }

            return new ContinuationPatternAnalysisState
            {
                PhaseCode = priorTrend == DirectionalTrend.Up ? "bullish_rectangle_forming" : "bearish_rectangle_forming",
                PhaseLabel = priorTrend == DirectionalTrend.Up ? "Rectangle de continuation haussier en formation" : "Rectangle de continuation baissier en formation",
                Status = PatternStatus.Monitoring,
                IsCompatible = true,
                StatusReason = "Le prix reste a l'interieur d'un rectangle avec tendance prealable exploitable, mais aucun breakout confirme n'est encore observe.",
                ValidationReason = "Une cloture hors range dans le sens de la tendance reste necessaire pour confirmer le pattern.",
                InvalidationReason = "Une sortie opposee ou une reintegration apres breakout affaiblirait la these de continuation.",
                Confidence = Math.Max(confidence, 0.48m),
                CurrentPrice = currentPrice,
                ReferencePrice = priorTrend == DirectionalTrend.Up ? resistance : support,
                InvalidationPrice = priorTrend == DirectionalTrend.Up ? support : resistance,
                StructuralPoints = PatternTechnicals.BuildBoundaryPoints(rangeWindow[^1].Date, ("support", support), ("resistance", resistance)),
                ScoreReasons = ["La congestion laterale est visible.", "Le range reste directionnellement neutre tant qu'il n'est pas casse dans le sens de la tendance prealable."]
            };
        }

        private static decimal BuildConfidence(bool hasStructure, DirectionalTrend priorTrend, bool breakoutUp, bool breakoutDown, int touchesResistance, int touchesSupport)
        {
            var confidence = 0.15m;
            if (hasStructure)
            {
                confidence += 0.20m;
            }

            if (priorTrend != DirectionalTrend.None)
            {
                confidence += 0.15m;
            }

            confidence += Math.Min(touchesResistance, 3) * 0.05m;
            confidence += Math.Min(touchesSupport, 3) * 0.05m;

            if (breakoutUp || breakoutDown)
            {
                confidence += 0.15m;
            }

            return PatternTechnicals.Clamp01(confidence);
        }

        private static DirectionalTrend ResolveDirectionalTrend(IReadOnlyList<TickerCandle> candles)
        {
            if (candles == null || candles.Count < 8)
            {
                return DirectionalTrend.None;
            }

            // Tendance prealable normalisee par la volatilite (multiple d'ATR) plutot qu'un
            // pourcentage fixe, coherente avec la detection du triangle.
            var move = candles[^1].Close - candles[0].Close;
            var threshold = PatternThresholds.PriorTrendMinMoveAtrMultiple * PatternTechnicals.VolatilityUnit(candles, candles[^1].Close);
            if (move >= threshold)
            {
                return DirectionalTrend.Up;
            }

            if (move <= -threshold)
            {
                return DirectionalTrend.Down;
            }

            return DirectionalTrend.None;
        }

        private enum DirectionalTrend
        {
            None,
            Up,
            Down
        }
    }
}
