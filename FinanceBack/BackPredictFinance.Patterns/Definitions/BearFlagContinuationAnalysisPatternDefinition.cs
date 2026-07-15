using BackPredictFinance.Common.AnalysisV1;
using BackPredictFinance.Common.enums;
using BackPredictFinance.Common.MarketData;
using BackPredictFinance.Patterns.Abstractions;
using BackPredictFinance.Patterns.Common;

namespace BackPredictFinance.Patterns.Definitions
{
    /// <summary>
    /// Miroir baissier de <see cref="BullFlagContinuationAnalysisPatternDefinition"/> : impulsion
    /// baissière (le "pole") suivie d'un rebond de consolidation court (le "flag"). Confirmé par un
    /// breakout sous le support du flag, invalidé si le prix repasse au-dessus de la résistance du
    /// flag ou si le rebond retrace trop de l'impulsion pour rester lisible comme simple pause.
    /// </summary>
    public sealed class BearFlagContinuationAnalysisPatternDefinition : ContinuationPatternDefinitionBase
    {
        public BearFlagContinuationAnalysisPatternDefinition(IPatternMarketDataProvider marketDataProvider)
            : base(marketDataProvider)
        {
        }

        public override string PatternId => PatternIds.BearFlagContinuation;
        public override string ModelVersion => "analysis-v1-bear-flag-continuation@lot3-atr";
        public override int HistoryLookbackMonths => 6;
        protected override int MinimumRequiredCandles => 40;
        protected override string DisplayName => "Bear flag continuation";
        protected override string PedagogicalDescription => "Impulsion baissiere suivie d'une consolidation courte et ordonnee, baissiere seulement apres breakout confirme sous le support du flag.";
        protected override decimal HistoricalReliability => BulkowskiReliability.BearFlag;

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
                    StatusReason = "Le moteur n'a pas reçu assez de bougies pour évaluer ce pattern de manière déterministe.",
                    ValidationReason = "Une profondeur historique minimale est requise avant toute validation.",
                    InvalidationReason = "Aucune invalidation n'est interpretable avec un historique insuffisant.",
                    Confidence = 0m,
                    CurrentPrice = candles[^1].Close,
                    ScoreReasons = ["Le nombre minimal de bougies n'est pas atteint pour ce pattern."]
                };
            }
            // Même découpage positionnel que le bull flag (12 bougies de pole + 10 de flag sur une
            // fenêtre de 22), en miroir baissier.
            var patternWindow = PatternTechnicals.Tail(candles, 22).ToList();
            var pole = patternWindow.Take(12).ToList();
            var flag = patternWindow.Skip(12).ToList();
            var currentPrice = candles[^1].Close;
            var poleDropPct = PatternTechnicals.ComputeReturnPct(pole[0].Close, pole[^1].Close);
            var poleHeight = pole.Max(candle => candle.High) - pole.Min(candle => candle.Low);
            var flagResistance = flag.Max(candle => candle.High);
            var flagSupport = flag.Min(candle => candle.Low);
            var flagHeight = flagResistance - flagSupport;
            // Retracement du rebond par rapport à la hauteur du pole (0 = le flag n'a rien repris de
            // la chute, 1 = le flag est remonté jusqu'en haut du pole).
            var flagRetracement = poleHeight <= 0m ? 1m : (flagResistance - pole[^1].Close) / poleHeight;
            var flagSlope = PatternTechnicals.ComputeSlope(flag.Select(candle => candle.Close).ToList());
            var averageFlagClose = PatternTechnicals.AverageClose(flag);
            var atr = PatternTechnicals.VolatilityUnit(patternWindow, currentPrice);
            // Marge de breakout exprimée en multiples d'ATR (adaptative à la volatilité du titre),
            // symétrique au bull flag.
            var breakoutDown = currentPrice < flagSupport - (PatternThresholds.BreakoutAtrMultiple * atr);
            var breakoutUp = currentPrice > flagResistance + (PatternThresholds.BreakoutAtrMultiple * atr);
            var hasStructure = poleDropPct <= -PatternThresholds.FlagMinPoleMovePct
                && poleHeight > 0m
                && averageFlagClose > 0m
                && flagHeight / averageFlagClose <= PatternThresholds.FlagMaxHeightRatio
                && flagRetracement <= PatternThresholds.FlagMaxRetracement
                && flagSlope >= -PatternThresholds.FlagMaxSlopeAtrPerCandle * atr;
            var confidence = BuildConfidence(hasStructure, breakoutDown, breakoutUp, poleDropPct, flagRetracement);

            if (!hasStructure)
            {
                return new ContinuationPatternAnalysisState
                {
                    PhaseCode = "flag_structure_not_confirmed",
                    PhaseLabel = "Structure de bear flag insuffisante",
                    Status = PatternStatus.Forming,
                    IsCompatible = false,
                    StatusReason = "L'impulsion baissiere initiale ou la consolidation courte contre-tendance ne sont pas suffisamment propres pour un bear flag.",
                    ValidationReason = "Le breakout ne peut pas etre interprete comme bear flag tant que l'impulsion et le flag ne sont pas lisibles.",
                    InvalidationReason = "Aucune invalidation exploitable tant que le bear flag n'est pas constitue.",
                    Confidence = confidence,
                    CurrentPrice = currentPrice,
                    ReferencePrice = flagSupport,
                    InvalidationPrice = flagResistance,
                    StructuralPoints = PatternTechnicals.BuildBoundaryPoints(flag[^1].Date, ("flag_support", flagSupport), ("flag_resistance", flagResistance)),
                    ScoreReasons = ["Le bear flag exige une impulsion baissiere nette suivie d'un rebond plus court et moins agressif."]
                };
            }

            if (breakoutDown)
            {
                return new ContinuationPatternAnalysisState
                {
                    PhaseCode = "bearish_breakout_confirmed",
                    PhaseLabel = "Breakout baissier confirme",
                    Status = PatternStatus.Confirmed,
                    IsCompatible = true,
                    StatusReason = "Le prix cloture sous le support du flag apres une impulsion baissiere initiale.",
                    IsValidated = true,
                    ValidationReason = "Le breakout valide la continuation baissiere du bear flag.",
                    ValidationRuleCode = "BEAR_FLAG_DOWNSIDE_BREAKOUT_CLOSE",
                    InvalidationReason = "Le scenario reste actif tant que le prix ne repasse pas au-dessus de la resistance du flag.",
                    Confidence = Math.Max(confidence, 0.76m),
                    CurrentPrice = currentPrice,
                    ReferencePrice = flagSupport,
                    TargetPrice = flagSupport - poleHeight,
                    InvalidationPrice = flagResistance,
                    StructuralPoints = PatternTechnicals.BuildBoundaryPoints(flag[^1].Date, ("flag_support", flagSupport), ("flag_resistance", flagResistance), ("breakout", currentPrice)),
                    ScoreReasons = ["Le pole baissier est present.", "Le flag reste modere face a l'impulsion.", "Le breakout par le bas confirme la continuation baissiere."]
                };
            }

            if (breakoutUp || flagRetracement > PatternThresholds.FlagMaxRetracement)
            {
                return new ContinuationPatternAnalysisState
                {
                    PhaseCode = "flag_resistance_broken",
                    PhaseLabel = "Resistance du flag rompue",
                    Status = PatternStatus.Invalidated,
                    IsCompatible = false,
                    StatusReason = "Le prix casse la resistance du flag ou le rebond devient trop profond pour un bear flag propre.",
                    ValidationReason = "Le scenario n'est plus compatible avec une continuation baissiere de bear flag.",
                    IsInvalidated = true,
                    InvalidationReason = "La rupture de resistance ou un rebond trop profond invalide la lecture de bear flag.",
                    InvalidationRuleCode = "BEAR_FLAG_RESISTANCE_FAILURE",
                    Confidence = 0.20m,
                    CurrentPrice = currentPrice,
                    ReferencePrice = flagResistance,
                    InvalidationPrice = flagResistance,
                    StructuralPoints = PatternTechnicals.BuildBoundaryPoints(flag[^1].Date, ("flag_support", flagSupport), ("flag_resistance", flagResistance), ("breakout", currentPrice)),
                    ScoreReasons = ["Le flag ne tient plus comme pause baissiere ordonnee."]
                };
            }

            return new ContinuationPatternAnalysisState
            {
                PhaseCode = "bear_flag_forming",
                PhaseLabel = "Bear flag en formation",
                Status = PatternStatus.Monitoring,
                IsCompatible = true,
                StatusReason = "L'impulsion baissiere et la consolidation courte restent compatibles avec un bear flag, sans breakout confirme pour l'instant.",
                ValidationReason = "Une cloture sous le support du flag reste necessaire pour confirmer la continuation.",
                InvalidationReason = "Une cassure au-dessus de la resistance du flag ou un rebond trop profond invaliderait le bear flag.",
                Confidence = Math.Max(confidence, 0.52m),
                CurrentPrice = currentPrice,
                ReferencePrice = flagSupport,
                InvalidationPrice = flagResistance,
                StructuralPoints = PatternTechnicals.BuildBoundaryPoints(flag[^1].Date, ("flag_support", flagSupport), ("flag_resistance", flagResistance)),
                ScoreReasons = ["Le pole baissier reste la base du pattern.", "Le flag reste une consolidation et non un retournement tant que sa resistance tient."]
            };
        }

        // Score additif borné [0,1], symétrique au bull flag : base faible, bonus pour structure
        // propre / pole marqué / flag resserré / breakout confirmé, malus si le prix casse déjà à la
        // hausse (invalidation potentielle du scénario baissier).
        private static decimal BuildConfidence(bool hasStructure, bool breakoutDown, bool breakoutUp, decimal poleDropPct, decimal flagRetracement)
        {
            var confidence = 0.15m;
            if (hasStructure)
            {
                confidence += 0.25m;
            }

            if (poleDropPct <= -PatternThresholds.FlagStrongPoleMovePct)
            {
                confidence += 0.15m;
            }

            if (flagRetracement <= PatternThresholds.FlagTightRetracement)
            {
                confidence += 0.10m;
            }

            if (breakoutDown)
            {
                confidence += 0.15m;
            }

            if (breakoutUp)
            {
                confidence -= 0.10m;
            }

            return PatternTechnicals.Clamp01(confidence);
        }
    }
}
