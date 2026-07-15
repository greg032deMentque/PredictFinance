using BackPredictFinance.Common.AnalysisV1;
using BackPredictFinance.Common.enums;
using BackPredictFinance.Common.MarketData;
using BackPredictFinance.Patterns.Abstractions;
using BackPredictFinance.Patterns.Common;

namespace BackPredictFinance.Patterns.Definitions
{
    /// <summary>
    /// Miroir haussier du double top : deux creux (pivots bas) de niveau quasi égal séparés par un
    /// rebond intermédiaire suffisant (la "neckline"), confirmé par une clôture au-dessus de la
    /// neckline. Cible mesurée = neckline + hauteur de la figure (measured move classique).
    /// </summary>
    public sealed class DoubleBottomReversalPatternDefinition : ReversalPatternDefinitionBase
    {
        public DoubleBottomReversalPatternDefinition(IPatternMarketDataProvider marketDataProvider)
            : base(marketDataProvider)
        {
        }

        public override string PatternId => PatternIds.DoubleBottom;
        public override string ModelVersion => "analysis-v1-double-bottom@m4";
        public override int HistoryLookbackMonths => 12;
        protected override int MinimumRequiredCandles => 60;
        protected override string DisplayName => "Double Bottom";
        protected override string PedagogicalDescription => "Deux creux a hauteur equivalente separes par un rebond intermediaire, signal de retournement haussier confirme par la cassure de la neckline.";
        protected override decimal HistoricalReliability => BulkowskiReliability.DoubleBottom;

        protected override ReversalPatternAnalysisState Analyze(AnalysisRequest request, IReadOnlyList<TickerCandle> candles)
        {
            if (candles.Count < MinimumRequiredCandles)
            {
                return BuildInsufficientHistoryState(candles[^1].Close);
            }

            var currentPrice = candles[^1].Close;
            var pivotIndices = PatternTechnicals.FindPivotLows(candles, PatternThresholds.PivotHalfWindow);

            var doubleBottomMatch = FindDoubleBottomPair(candles, pivotIndices);

            if (doubleBottomMatch is null)
            {
                return BuildNotConfirmedState(currentPrice);
            }

            var (firstIndex, secondIndex, low1, low2, neckline) = doubleBottomMatch.Value;
            var figureHeight = neckline - ((low1 + low2) / 2m);
            var targetPrice = neckline + figureHeight;
            var invalidationPrice = Math.Min(low1, low2);
            var hasVolumeExpansion = HasRecentVolumeExpansion(candles);
            var latestDate = candles[^1].Date;

            var structuralPoints = PatternTechnicals.BuildBoundaryPoints(
                latestDate,
                ("double_bottom_low1", low1),
                ("double_bottom_low2", low2),
                ("neckline", neckline));

            if (currentPrice > neckline)
            {
                return BuildBreakoutConfirmedState(
                    currentPrice, neckline, targetPrice, invalidationPrice,
                    firstIndex, secondIndex, structuralPoints, hasVolumeExpansion);
            }

            return BuildFormingState(
                currentPrice, neckline, targetPrice, invalidationPrice,
                firstIndex, secondIndex, structuralPoints, hasVolumeExpansion);
        }

        // Contrairement au double top (qui scanne du pivot le plus récent vers le plus ancien), ce
        // scan part du pivot le plus ANCIEN et avance vers le présent, retournant la première paire
        // valide trouvée. Les deux figures ne privilégient donc pas symétriquement "la paire la plus
        // récente" — une divergence assumée entre les deux implémentations, à garder en tête si un
        // écart de comportement est observé entre double top et double bottom sur un même historique.
        private static (int FirstIndex, int SecondIndex, decimal Low1, decimal Low2, decimal Neckline)?
            FindDoubleBottomPair(IReadOnlyList<TickerCandle> candles, List<int> pivotIndices)
        {
            var minIndexSeparation = (int)Math.Ceiling(PatternThresholds.DoubleMinSeparationAtrMultiple);

            for (var outerIndex = 0; outerIndex < pivotIndices.Count - 1; outerIndex++)
            {
                for (var innerIndex = outerIndex + 1; innerIndex < pivotIndices.Count; innerIndex++)
                {
                    var firstIndex = pivotIndices[outerIndex];
                    var secondIndex = pivotIndices[innerIndex];
                    var low1 = candles[firstIndex].Low;
                    var low2 = candles[secondIndex].Low;

                    if (!PatternTechnicals.ArePricesEqual(low1, low2))
                    {
                        continue;
                    }

                    var indexSpan = secondIndex - firstIndex;
                    if (indexSpan <= minIndexSeparation)
                    {
                        continue;
                    }

                    var neckline = ComputeNeckline(candles, firstIndex, secondIndex);
                    var figureHeight = neckline - ((low1 + low2) / 2m);

                    if (figureHeight <= 0m)
                    {
                        continue;
                    }

                    var intermediateHigh = ComputeIntermediateHigh(candles, firstIndex, secondIndex);
                    var reboundHeight = intermediateHigh - Math.Min(low1, low2);
                    var reboundRatio = figureHeight > 0m ? reboundHeight / figureHeight : 0m;

                    if (reboundRatio < PatternThresholds.DoubleMinIntermediateReboundPct)
                    {
                        continue;
                    }

                    return (firstIndex, secondIndex, low1, low2, neckline);
                }
            }

            return null;
        }

        // Neckline = plus haut close entre les deux creux (symétrique au "plus bas close" du double
        // top) : la résistance à franchir pour valider le retournement haussier.
        private static decimal ComputeNeckline(IReadOnlyList<TickerCandle> candles, int firstIndex, int secondIndex)
        {
            var maxClose = candles[firstIndex].Close;

            for (var i = firstIndex + 1; i <= secondIndex; i++)
            {
                if (candles[i].Close > maxClose)
                {
                    maxClose = candles[i].Close;
                }
            }

            return maxClose;
        }

        private static decimal ComputeIntermediateHigh(IReadOnlyList<TickerCandle> candles, int firstIndex, int secondIndex)
        {
            var maxHigh = candles[firstIndex].High;

            for (var i = firstIndex + 1; i < secondIndex; i++)
            {
                if (candles[i].High > maxHigh)
                {
                    maxHigh = candles[i].High;
                }
            }

            return maxHigh;
        }

        private static bool HasRecentVolumeExpansion(IReadOnlyList<TickerCandle> candles)
            => PatternTechnicals.IsVolumeExpanding(candles);

        // Confiance fixée par palier selon la phase (plutôt qu'un score additif comme les flags) :
        // 0.80 une fois le breakout confirmé, 0.65 en formation (structure valide, pas encore cassée),
        // 0.50 en dernier recours si BuildConfidence est appelé sans phase reconnue.
        private static decimal BuildConfidence(string phase, bool hasVolumeExpansion)
        {
            var confidence = phase switch
            {
                "double_bottom_breakout_confirmed" => 0.80m,
                "double_bottom_forming" => 0.65m,
                _ => 0.50m
            };

            if (hasVolumeExpansion)
            {
                confidence += 0.05m;
            }

            return PatternTechnicals.Clamp01(confidence);
        }

        private static ReversalPatternAnalysisState BuildInsufficientHistoryState(decimal currentPrice)
        {
            return new ReversalPatternAnalysisState
            {
                PhaseCode = "insufficient_history",
                PhaseLabel = "Historique insuffisant",
                Status = PatternStatus.Forming,
                IsCompatible = false,
                StatusReason = "Le moteur n'a pas reçu assez de bougies pour évaluer ce pattern de manière déterministe.",
                ValidationReason = "Une profondeur historique minimale est requise avant toute validation.",
                InvalidationReason = "Aucune invalidation n'est interpretable avec un historique insuffisant.",
                Confidence = 0m,
                CurrentPrice = currentPrice,
                ScoreReasons = ["Le nombre minimal de bougies n'est pas atteint pour ce pattern."]
            };
        }

        private static ReversalPatternAnalysisState BuildNotConfirmedState(decimal currentPrice)
        {
            return new ReversalPatternAnalysisState
            {
                PhaseCode = "double_bottom_not_confirmed",
                PhaseLabel = "Double Bottom non identifie",
                Status = PatternStatus.Forming,
                IsCompatible = false,
                StatusReason = "Aucune paire de creux equivalents avec rebond intermediaire suffisant n'a ete detectee.",
                ValidationReason = "La structure double bottom n'est pas constituee.",
                InvalidationReason = "Aucune invalidation exploitable tant que la figure n'est pas constituee.",
                Confidence = BuildConfidence("double_bottom_not_confirmed", false),
                CurrentPrice = currentPrice,
                ScoreReasons = ["Pas de deux creux a hauteur equivalente (ecart <= 6 %) avec rebond intermediaire >= 10 % de la figure."]
            };
        }

        private static ReversalPatternAnalysisState BuildFormingState(
            decimal currentPrice,
            decimal neckline,
            decimal targetPrice,
            decimal invalidationPrice,
            int firstIndex,
            int secondIndex,
            List<PatternStructuralPoint> structuralPoints,
            bool hasVolumeExpansion)
        {
            var scoreReasons = new List<string>
            {
                "Deux creux equivalents identifies avec rebond intermediaire suffisant.",
                "La neckline n'est pas encore franchie : attente de confirmation."
            };

            if (hasVolumeExpansion)
            {
                scoreReasons.Add("Volume recent superieur a la moyenne : signal d'intensification des acheteurs.");
            }

            return new ReversalPatternAnalysisState
            {
                PhaseCode = "double_bottom_forming",
                PhaseLabel = "Double Bottom en formation",
                Status = PatternStatus.Monitoring,
                IsCompatible = true,
                StatusReason = "Les deux creux sont identifies et equivalents. La cassure de la neckline reste a confirmer.",
                ValidationReason = "Une cloture au-dessus de la neckline est necessaire pour confirmer le retournement.",
                InvalidationReason = "Une cassure sous le plus bas des deux creux invaliderait la figure.",
                Confidence = BuildConfidence("double_bottom_forming", hasVolumeExpansion),
                CurrentPrice = currentPrice,
                NecklinePrice = neckline,
                TargetPrice = targetPrice,
                InvalidationPrice = invalidationPrice,
                FirstPeakIndex = firstIndex,
                SecondPeakIndex = secondIndex,
                StructuralPoints = structuralPoints,
                ScoreReasons = scoreReasons
            };
        }

        private static ReversalPatternAnalysisState BuildBreakoutConfirmedState(
            decimal currentPrice,
            decimal neckline,
            decimal targetPrice,
            decimal invalidationPrice,
            int firstIndex,
            int secondIndex,
            List<PatternStructuralPoint> structuralPoints,
            bool hasVolumeExpansion)
        {
            var scoreReasons = new List<string>
            {
                "Les deux creux equivalents sont confirmes.",
                "Le prix cloture au-dessus de la neckline : retournement haussier confirme.",
                "Cible mesuree = neckline + hauteur de la figure (measured move)."
            };

            if (hasVolumeExpansion)
            {
                scoreReasons.Add("Volume recent superieur a la moyenne : breakout soutenu par les volumes.");
            }

            return new ReversalPatternAnalysisState
            {
                PhaseCode = "double_bottom_breakout_confirmed",
                PhaseLabel = "Breakout haussier confirme",
                Status = PatternStatus.Confirmed,
                IsCompatible = true,
                IsValidated = true,
                StatusReason = "Le prix cloture au-dessus de la neckline apres formation d'un double bottom valide.",
                ValidationReason = "La cassure de la neckline confirme le retournement haussier du double bottom.",
                ValidationRuleCode = "DOUBLE_BOTTOM_NECKLINE_BREAKOUT_CLOSE",
                InvalidationReason = "Le scenario reste actif tant que le prix ne repasse pas sous le plus bas des deux creux.",
                Confidence = BuildConfidence("double_bottom_breakout_confirmed", hasVolumeExpansion),
                CurrentPrice = currentPrice,
                NecklinePrice = neckline,
                TargetPrice = targetPrice,
                InvalidationPrice = invalidationPrice,
                FirstPeakIndex = firstIndex,
                SecondPeakIndex = secondIndex,
                StructuralPoints = structuralPoints,
                ScoreReasons = scoreReasons
            };
        }
    }
}
