using BackPredictFinance.Common.AnalysisV1;
using BackPredictFinance.Common.enums;
using BackPredictFinance.Common.MarketData;
using BackPredictFinance.Patterns.Abstractions;
using BackPredictFinance.Patterns.Common;

namespace BackPredictFinance.Patterns.Definitions
{
    /// <summary>
    /// Miroir haussier du tête-épaules : trois creux (pivots bas) où la tête est plus profonde que
    /// les deux épaules, celles-ci restant temporellement symétriques. La neckline relie ici les deux
    /// hauts intermédiaires (et non les creux) ; confirmé par une clôture au-dessus de cette neckline.
    /// </summary>
    public sealed class InverseHeadAndShouldersReversalPatternDefinition : ReversalPatternDefinitionBase
    {
        public InverseHeadAndShouldersReversalPatternDefinition(IPatternMarketDataProvider marketDataProvider)
            : base(marketDataProvider)
        {
        }

        public override string PatternId => PatternIds.InverseHeadAndShoulders;
        public override string ModelVersion => "analysis-v1-inverse-hs@m4";
        public override int HistoryLookbackMonths => 12;
        protected override int MinimumRequiredCandles => 80;
        protected override string DisplayName => "Tete-Epaules Inverse";
        protected override string PedagogicalDescription => "Trois creux formes d'une tete plus basse encadree de deux epaules, signal de retournement haussier confirme par la cassure de la neckline tracee par les hauts intermediaires.";
        protected override decimal HistoricalReliability => BulkowskiReliability.InverseHeadAndShoulders;

        protected override ReversalPatternAnalysisState Analyze(AnalysisRequest request, IReadOnlyList<TickerCandle> candles)
        {
            if (candles.Count < MinimumRequiredCandles)
            {
                return BuildInsufficientHistoryState(candles[^1].Close);
            }

            var currentPrice = candles[^1].Close;
            var pivotIndices = PatternTechnicals.FindPivotLows(candles, PatternThresholds.PivotHalfWindow);

            var match = FindInverseHsTriple(candles, pivotIndices);

            if (match is null)
            {
                return BuildNotConfirmedState(currentPrice);
            }

            var (leftShoulderIndex, headIndex, rightShoulderIndex, neckline) = match.Value;
            var headLow = candles[headIndex].Low;
            var figureHeight = neckline - headLow;
            var targetPrice = neckline + figureHeight;
            var invalidationPrice = headLow;
            var hasVolumeExpansion = HasRecentVolumeExpansion(candles);
            var latestDate = candles[^1].Date;

            var structuralPoints = PatternTechnicals.BuildBoundaryPoints(
                latestDate,
                ("inverse_hs_left_shoulder", candles[leftShoulderIndex].Low),
                ("inverse_hs_head", headLow),
                ("inverse_hs_right_shoulder", candles[rightShoulderIndex].Low),
                ("neckline", neckline));

            if (currentPrice > neckline)
            {
                return BuildBreakoutConfirmedState(
                    currentPrice, neckline, targetPrice, invalidationPrice,
                    leftShoulderIndex, headIndex, rightShoulderIndex,
                    structuralPoints, hasVolumeExpansion);
            }

            return BuildFormingState(
                currentPrice, neckline, targetPrice, invalidationPrice,
                leftShoulderIndex, headIndex, rightShoulderIndex,
                structuralPoints, hasVolumeExpansion);
        }

        // Même stratégie de scan que le tête-épaules haussier standard : triple boucle chronologique
        // croissante, on retient le premier triplet valide (épaule gauche la plus ancienne d'abord),
        // pas nécessairement le plus symétrique ni le plus récent.
        private static (int LeftShoulderIndex, int HeadIndex, int RightShoulderIndex, decimal Neckline)?
            FindInverseHsTriple(IReadOnlyList<TickerCandle> candles, List<int> pivotIndices)
        {
            for (var lsIdx = 0; lsIdx < pivotIndices.Count - 2; lsIdx++)
            {
                for (var hIdx = lsIdx + 1; hIdx < pivotIndices.Count - 1; hIdx++)
                {
                    for (var rsIdx = hIdx + 1; rsIdx < pivotIndices.Count; rsIdx++)
                    {
                        var result = EvaluateTriple(candles, pivotIndices[lsIdx], pivotIndices[hIdx], pivotIndices[rsIdx]);
                        if (result.HasValue)
                        {
                            return result;
                        }
                    }
                }
            }

            return null;
        }

        private static (int LeftShoulderIndex, int HeadIndex, int RightShoulderIndex, decimal Neckline)?
            EvaluateTriple(IReadOnlyList<TickerCandle> candles, int leftShoulderIndex, int headIndex, int rightShoulderIndex)
        {
            var leftShoulderLow = candles[leftShoulderIndex].Low;
            var headLow = candles[headIndex].Low;
            var rightShoulderLow = candles[rightShoulderIndex].Low;

            if (!IsHeadDeeper(headLow, leftShoulderLow, rightShoulderLow))
            {
                return null;
            }

            // Neckline calculee AVANT le test de profondeur : figureHeight doit mesurer la distance
            // neckline -> tete (miroir exact du tete-epaules haussier), jamais avgShoulder -> tete
            // (qui rendrait le ratio de profondeur algebriquement egal a 1, voir HeadAndShoulders...).
            var neckline = ComputeNeckline(candles, leftShoulderIndex, headIndex, rightShoulderIndex);
            var figureHeight = neckline - headLow;

            if (figureHeight <= 0m)
            {
                return null;
            }

            var avgShoulder = (leftShoulderLow + rightShoulderLow) / 2m;

            if (!IsHeadDepthSufficient(neckline, avgShoulder, figureHeight))
            {
                return null;
            }

            if (!IsShoulderSymmetric(leftShoulderIndex, headIndex, rightShoulderIndex))
            {
                return null;
            }

            return (leftShoulderIndex, headIndex, rightShoulderIndex, neckline);
        }

        private static bool IsHeadDeeper(decimal headLow, decimal leftShoulderLow, decimal rightShoulderLow)
        {
            return headLow < leftShoulderLow && headLow < rightShoulderLow;
        }

        // Miroir exact de HeadAndShouldersReversalPatternDefinition.FindHeadAndShouldersTriplet :
        // depthRatio = (neckline - avgShoulder) / figureHeight, ou figureHeight = neckline - headLow.
        // Ce sont deux quantites distinctes (contrairement a l'ancienne version qui comparait
        // avgShoulder - headLow a elle-meme), donc le ratio n'est plus structurellement fixe a 1.
        private static bool IsHeadDepthSufficient(decimal neckline, decimal avgShoulder, decimal figureHeight)
        {
            var depthRatio = (neckline - avgShoulder) / figureHeight;
            return depthRatio >= PatternThresholds.HsMinHeadDepthRatio;
        }

        private static bool IsShoulderSymmetric(int leftShoulderIndex, int headIndex, int rightShoulderIndex)
        {
            var leftWidth = headIndex - leftShoulderIndex;
            var rightWidth = rightShoulderIndex - headIndex;

            if (leftWidth <= 0 || rightWidth <= 0)
            {
                return false;
            }

            var ratio = (decimal)leftWidth / rightWidth;
            var lowerBound = 1m / PatternThresholds.HsShoulderSymmetryRatio;
            var upperBound = PatternThresholds.HsShoulderSymmetryRatio;

            return ratio >= lowerBound && ratio <= upperBound;
        }

        // Ici la neckline relie les deux HAUTS intermédiaires (entre épaule et tête) plutôt que les
        // creux : logique, puisque la figure inversée cherche une résistance à franchir vers le haut,
        // symétrique de la neckline "basse" du tête-épaules standard.
        private static decimal ComputeNeckline(
            IReadOnlyList<TickerCandle> candles,
            int leftShoulderIndex,
            int headIndex,
            int rightShoulderIndex)
        {
            var leftIntermedHigh = ComputeIntermedHigh(candles, leftShoulderIndex, headIndex);
            var rightIntermedHigh = ComputeIntermedHigh(candles, headIndex, rightShoulderIndex);

            return (leftIntermedHigh + rightIntermedHigh) / 2m;
        }

        private static decimal ComputeIntermedHigh(IReadOnlyList<TickerCandle> candles, int fromIndex, int toIndex)
        {
            var maxHigh = candles[fromIndex].High;

            for (var i = fromIndex + 1; i <= toIndex; i++)
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

        private static decimal BuildConfidence(string phase, bool hasVolumeExpansion)
        {
            var confidence = phase switch
            {
                "inverse_hs_breakout_confirmed" => 0.80m,
                "inverse_hs_forming" => 0.65m,
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
                PhaseCode = "inverse_hs_not_confirmed",
                PhaseLabel = "Tete-Epaules Inverse non identifie",
                Status = PatternStatus.Forming,
                IsCompatible = false,
                StatusReason = "Aucun triplet valide (epaule gauche, tete plus basse, epaule droite symetrique) n'a ete detecte.",
                ValidationReason = "La structure tete-epaules inverse n'est pas constituee.",
                InvalidationReason = "Aucune invalidation exploitable tant que la figure n'est pas constituee.",
                Confidence = BuildConfidence("inverse_hs_not_confirmed", false),
                CurrentPrice = currentPrice,
                ScoreReasons = ["Pas de triplet creux avec tete plus profonde que les epaules et symetrie 0.8-1.2."]
            };
        }

        private static ReversalPatternAnalysisState BuildFormingState(
            decimal currentPrice,
            decimal neckline,
            decimal targetPrice,
            decimal invalidationPrice,
            int leftShoulderIndex,
            int headIndex,
            int rightShoulderIndex,
            List<PatternStructuralPoint> structuralPoints,
            bool hasVolumeExpansion)
        {
            var scoreReasons = new List<string>
            {
                "Triplet valide : tete plus profonde que les deux epaules, symetrie dans les tolerances Bulkowski.",
                "La neckline n'est pas encore franchie : attente de confirmation."
            };

            if (hasVolumeExpansion)
            {
                scoreReasons.Add("Volume recent superieur a la moyenne : signal d'intensification des acheteurs.");
            }

            return new ReversalPatternAnalysisState
            {
                PhaseCode = "inverse_hs_forming",
                PhaseLabel = "Tete-Epaules Inverse en formation",
                Status = PatternStatus.Monitoring,
                IsCompatible = true,
                StatusReason = "Le triplet est identifie. La cassure de la neckline reste a confirmer.",
                ValidationReason = "Une cloture au-dessus de la neckline est necessaire pour confirmer le retournement haussier.",
                InvalidationReason = "Un retour sous le plus bas de la tete invaliderait la figure.",
                Confidence = BuildConfidence("inverse_hs_forming", hasVolumeExpansion),
                CurrentPrice = currentPrice,
                NecklinePrice = neckline,
                TargetPrice = targetPrice,
                InvalidationPrice = invalidationPrice,
                LeftShoulderIndex = leftShoulderIndex,
                HeadIndex = headIndex,
                RightShoulderIndex = rightShoulderIndex,
                StructuralPoints = structuralPoints,
                ScoreReasons = scoreReasons
            };
        }

        private static ReversalPatternAnalysisState BuildBreakoutConfirmedState(
            decimal currentPrice,
            decimal neckline,
            decimal targetPrice,
            decimal invalidationPrice,
            int leftShoulderIndex,
            int headIndex,
            int rightShoulderIndex,
            List<PatternStructuralPoint> structuralPoints,
            bool hasVolumeExpansion)
        {
            var scoreReasons = new List<string>
            {
                "Triplet valide : tete plus profonde que les deux epaules, symetrie Bulkowski confirmee.",
                "Le prix cloture au-dessus de la neckline : retournement haussier confirme.",
                "Cible mesuree = neckline + hauteur de la figure (measured move)."
            };

            if (hasVolumeExpansion)
            {
                scoreReasons.Add("Volume recent superieur a la moyenne : breakout soutenu par les volumes.");
            }

            return new ReversalPatternAnalysisState
            {
                PhaseCode = "inverse_hs_breakout_confirmed",
                PhaseLabel = "Breakout haussier confirme",
                Status = PatternStatus.Confirmed,
                IsCompatible = true,
                IsValidated = true,
                StatusReason = "Le prix cloture au-dessus de la neckline apres formation d'un tete-epaules inverse valide.",
                ValidationReason = "La cassure de la neckline confirme le retournement haussier du tete-epaules inverse.",
                ValidationRuleCode = "INVERSE_HS_NECKLINE_BREAKOUT_CLOSE",
                InvalidationReason = "Le scenario reste actif tant que le prix ne repasse pas sous le plus bas de la tete.",
                Confidence = BuildConfidence("inverse_hs_breakout_confirmed", hasVolumeExpansion),
                CurrentPrice = currentPrice,
                NecklinePrice = neckline,
                TargetPrice = targetPrice,
                InvalidationPrice = invalidationPrice,
                LeftShoulderIndex = leftShoulderIndex,
                HeadIndex = headIndex,
                RightShoulderIndex = rightShoulderIndex,
                StructuralPoints = structuralPoints,
                ScoreReasons = scoreReasons
            };
        }
    }
}
