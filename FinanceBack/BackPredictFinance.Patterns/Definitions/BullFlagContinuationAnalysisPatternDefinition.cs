using BackPredictFinance.Common.AnalysisV1;
using BackPredictFinance.Common.enums;
using BackPredictFinance.Common.MarketData;
using BackPredictFinance.Patterns.Abstractions;
using BackPredictFinance.Patterns.Common;

namespace BackPredictFinance.Patterns.Definitions
{
    /// <summary>
    /// Détecte un "bull flag" : une impulsion haussière nette (le "pole") suivie d'une consolidation
    /// courte et peu profonde à contre-tendance (le "flag"), interprétée comme une pause avant
    /// continuation. Le scénario n'est confirmé qu'après un breakout par le haut du flag ; il est
    /// invalidé si le support du flag casse ou si le retracement devient trop profond pour rester
    /// lisible comme pause (auquel cas ce n'est plus une consolidation mais un retournement).
    /// </summary>
    public sealed class BullFlagContinuationAnalysisPatternDefinition : ContinuationPatternDefinitionBase
    {
        public BullFlagContinuationAnalysisPatternDefinition(IPatternMarketDataProvider marketDataProvider)
            : base(marketDataProvider)
        {
        }

        public override string PatternId => PatternIds.BullFlagContinuation;
        public override string ModelVersion => "analysis-v1-bull-flag-continuation@lot3-atr";
        public override int HistoryLookbackMonths => 6;
        protected override int MinimumRequiredCandles => 40;
        protected override string DisplayName => "Bull flag continuation";
        protected override string PedagogicalDescription => "Impulsion haussiere suivie d'une consolidation courte et ordonnee, haussiere seulement apres breakout confirme par le haut du flag.";
        protected override decimal HistoricalReliability => BulkowskiReliability.BullFlag;

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
            // Fenêtre fixe de 22 bougies découpée en deux blocs chronologiques : les 12 premières
            // forment le "pole" (impulsion), les 10 suivantes le "flag" (consolidation). Ce découpage
            // est positionnel, pas détecté dynamiquement — un pole plus court/long dans la réalité
            // sera quand même mesuré sur exactement 12 bougies, ce qui est une simplification assumée.
            var patternWindow = PatternTechnicals.Tail(candles, 22).ToList();
            var pole = patternWindow.Take(12).ToList();
            var flag = patternWindow.Skip(12).ToList();
            var currentPrice = candles[^1].Close;
            var poleGainPct = PatternTechnicals.ComputeReturnPct(pole[0].Close, pole[^1].Close);
            var poleHeight = pole.Max(candle => candle.High) - pole.Min(candle => candle.Low);
            var flagResistance = flag.Max(candle => candle.High);
            var flagSupport = flag.Min(candle => candle.Low);
            var flagHeight = flagResistance - flagSupport;
            // Profondeur du retracement du flag par rapport à la hauteur du pole (0 = le flag n'a rien
            // repris de l'impulsion, 1 = le flag est redescendu jusqu'au bas du pole). Un flag propre
            // reste "haut" sur cette échelle (retracement faible).
            var flagRetracement = poleHeight <= 0m ? 1m : (pole[^1].Close - flagSupport) / poleHeight;
            var flagSlope = PatternTechnicals.ComputeSlope(flag.Select(candle => candle.Close).ToList());
            var averageFlagClose = PatternTechnicals.AverageClose(flag);
            var atr = PatternTechnicals.VolatilityUnit(patternWindow, currentPrice);
            // Le breakout doit dépasser la résistance/le support d'une marge exprimée en multiples
            // d'ATR (et non un seuil de prix fixe) : le seuil s'adapte ainsi à la volatilité propre du
            // titre plutôt que de générer de faux signaux sur les titres peu volatils.
            var breakoutUp = currentPrice > flagResistance + (PatternThresholds.BreakoutAtrMultiple * atr);
            var breakdownDown = currentPrice < flagSupport - (PatternThresholds.BreakoutAtrMultiple * atr);
            // Un bull flag "propre" exige simultanément : une impulsion suffisante, une consolidation
            // resserrée par rapport au prix (flagHeight/averageFlagClose), un retracement contenu, et
            // une pente de flag qui ne s'effondre pas trop vite (mesurée en unités d'ATR par bougie).
            var hasStructure = poleGainPct >= PatternThresholds.FlagMinPoleMovePct
                && poleHeight > 0m
                && averageFlagClose > 0m
                && flagHeight / averageFlagClose <= PatternThresholds.FlagMaxHeightRatio
                && flagRetracement <= PatternThresholds.FlagMaxRetracement
                && flagSlope <= PatternThresholds.FlagMaxSlopeAtrPerCandle * atr;
            var confidence = BuildConfidence(hasStructure, breakoutUp, breakdownDown, poleGainPct, flagRetracement);

            if (!hasStructure)
            {
                return new ContinuationPatternAnalysisState
                {
                    PhaseCode = "flag_structure_not_confirmed",
                    PhaseLabel = "Structure de bull flag insuffisante",
                    Status = PatternStatus.Forming,
                    IsCompatible = false,
                    StatusReason = "L'impulsion initiale ou la consolidation courte contre-tendance ne sont pas suffisamment propres pour un bull flag.",
                    ValidationReason = "Le breakout ne peut pas etre interprete comme bull flag tant que l'impulsion et le flag ne sont pas lisibles.",
                    InvalidationReason = "Aucune invalidation exploitable tant que le bull flag n'est pas constitue.",
                    Confidence = confidence,
                    CurrentPrice = currentPrice,
                    ReferencePrice = flagResistance,
                    InvalidationPrice = flagSupport,
                    StructuralPoints = PatternTechnicals.BuildBoundaryPoints(flag[^1].Date, ("flag_resistance", flagResistance), ("flag_support", flagSupport)),
                    ScoreReasons = ["Le bull flag exige une impulsion haussiere nette suivie d'une consolidation plus courte et moins agressive."]
                };
            }

            if (breakoutUp)
            {
                return new ContinuationPatternAnalysisState
                {
                    PhaseCode = "bullish_breakout_confirmed",
                    PhaseLabel = "Breakout haussier confirme",
                    Status = PatternStatus.Confirmed,
                    IsCompatible = true,
                    StatusReason = "Le prix cloture au-dessus de la resistance du flag apres une impulsion haussiere initiale.",
                    IsValidated = true,
                    ValidationReason = "Le breakout valide la continuation haussiere du bull flag.",
                    ValidationRuleCode = "BULL_FLAG_UPSIDE_BREAKOUT_CLOSE",
                    InvalidationReason = "Le scenario reste actif tant que le prix ne repasse pas sous le support du flag.",
                    Confidence = Math.Max(confidence, 0.76m),
                    CurrentPrice = currentPrice,
                    ReferencePrice = flagResistance,
                    TargetPrice = flagResistance + poleHeight,
                    InvalidationPrice = flagSupport,
                    StructuralPoints = PatternTechnicals.BuildBoundaryPoints(flag[^1].Date, ("flag_resistance", flagResistance), ("flag_support", flagSupport), ("breakout", currentPrice)),
                    ScoreReasons = ["Le pole haussier est present.", "Le flag reste modere face a l'impulsion.", "Le breakout par le haut confirme la continuation haussiere."]
                };
            }

            if (breakdownDown || flagRetracement > PatternThresholds.FlagMaxRetracement)
            {
                return new ContinuationPatternAnalysisState
                {
                    PhaseCode = "flag_support_broken",
                    PhaseLabel = "Support du flag rompu",
                    Status = PatternStatus.Invalidated,
                    IsCompatible = false,
                    StatusReason = "Le prix casse le support du flag ou le retracement devient trop profond pour un bull flag propre.",
                    ValidationReason = "Le scenario n'est plus compatible avec une continuation haussiere de bull flag.",
                    IsInvalidated = true,
                    InvalidationReason = "La rupture du support ou un retracement trop profond invalide la lecture de bull flag.",
                    InvalidationRuleCode = "BULL_FLAG_SUPPORT_FAILURE",
                    Confidence = 0.20m,
                    CurrentPrice = currentPrice,
                    ReferencePrice = flagSupport,
                    InvalidationPrice = flagSupport,
                    StructuralPoints = PatternTechnicals.BuildBoundaryPoints(flag[^1].Date, ("flag_resistance", flagResistance), ("flag_support", flagSupport), ("breakdown", currentPrice)),
                    ScoreReasons = ["Le flag ne tient plus comme pause haussiere ordonnee."]
                };
            }

            return new ContinuationPatternAnalysisState
            {
                PhaseCode = "bull_flag_forming",
                PhaseLabel = "Bull flag en formation",
                Status = PatternStatus.Monitoring,
                IsCompatible = true,
                StatusReason = "L'impulsion haussiere et la consolidation courte restent compatibles avec un bull flag, sans breakout confirme pour l'instant.",
                ValidationReason = "Une cloture au-dessus de la resistance du flag reste necessaire pour confirmer la continuation.",
                InvalidationReason = "Une cassure sous le support du flag ou un retracement trop profond invaliderait le bull flag.",
                Confidence = Math.Max(confidence, 0.52m),
                CurrentPrice = currentPrice,
                ReferencePrice = flagResistance,
                InvalidationPrice = flagSupport,
                StructuralPoints = PatternTechnicals.BuildBoundaryPoints(flag[^1].Date, ("flag_resistance", flagResistance), ("flag_support", flagSupport)),
                ScoreReasons = ["Le pole haussier reste la base du pattern.", "Le flag reste une consolidation et non un renversement tant que son support tient."]
            };
        }

        // Score additif borné [0,1] (voir Clamp01) : base faible (0.15) tant que rien n'est confirmé,
        // bonus cumulés pour une structure propre / un pole fort / un flag resserré / un breakout
        // confirmé, malus si le prix casse déjà à la baisse (signal contradictoire avec le scénario).
        private static decimal BuildConfidence(bool hasStructure, bool breakoutUp, bool breakdownDown, decimal poleGainPct, decimal flagRetracement)
        {
            var confidence = 0.15m;
            if (hasStructure)
            {
                confidence += 0.25m;
            }

            if (poleGainPct >= PatternThresholds.FlagStrongPoleMovePct)
            {
                confidence += 0.15m;
            }

            if (flagRetracement <= PatternThresholds.FlagTightRetracement)
            {
                confidence += 0.10m;
            }

            if (breakoutUp)
            {
                confidence += 0.15m;
            }

            if (breakdownDown)
            {
                confidence -= 0.10m;
            }

            return PatternTechnicals.Clamp01(confidence);
        }
    }
}
