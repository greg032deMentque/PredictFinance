using BackPredictFinance.Common.AnalysisV1;
using BackPredictFinance.Common.enums;
using BackPredictFinance.Common.MarketData;
using BackPredictFinance.Patterns.Abstractions;
using BackPredictFinance.Patterns.Common;

namespace BackPredictFinance.Patterns.Definitions
{
    /// <summary>
    /// Détecte une figure tête-épaules : trois sommets consécutifs (épaule gauche, tête, épaule
    /// droite) où la tête dépasse nettement les deux épaules et où celles-ci sont temporellement
    /// à peu près symétriques autour de la tête. Confirmé par une clôture sous la neckline (moyenne
    /// des deux creux qui séparent les sommets), invalidé par un retour du prix au-dessus de la tête.
    /// </summary>
    public sealed class HeadAndShouldersReversalPatternDefinition : ReversalPatternDefinitionBase
    {
        public HeadAndShouldersReversalPatternDefinition(IPatternMarketDataProvider marketDataProvider)
            : base(marketDataProvider)
        {
        }

        public override string PatternId => PatternIds.HeadAndShoulders;
        public override string ModelVersion => "analysis-v1-head-and-shoulders@m4";
        public override int HistoryLookbackMonths => 12;
        protected override int MinimumRequiredCandles => 80;
        protected override string DisplayName => "Tete-Epaules";
        protected override string PedagogicalDescription => "Trois sommets dont la tete depasse nettement les deux epaules, signal de retournement baissier confirme par la cassure sous la neckline.";
        protected override decimal HistoricalReliability => BulkowskiReliability.HeadAndShoulders;

        protected override ReversalPatternAnalysisState Analyze(AnalysisRequest request, IReadOnlyList<TickerCandle> candles)
        {
            if (candles.Count < MinimumRequiredCandles)
            {
                return BuildInsufficientHistoryState(candles[^1].Close);
            }

            var currentPrice = candles[^1].Close;
            var pivotHighIndices = PatternTechnicals.FindPivotHighs(candles, PatternThresholds.PivotHalfWindow);

            var triplet = FindHeadAndShouldersTriplet(candles, pivotHighIndices);

            if (triplet is null)
            {
                return BuildNotConfirmedState(currentPrice);
            }

            var (leftShoulderIndex, headIndex, rightShoulderIndex, neckline) = triplet.Value;
            var headHigh = candles[headIndex].High;
            var figureHeight = headHigh - neckline;
            var targetPrice = neckline - figureHeight;
            var invalidationPrice = headHigh;
            var hasVolumeExpansion = HasRecentVolumeExpansion(candles);
            var latestDate = candles[^1].Date;

            var structuralPoints = PatternTechnicals.BuildBoundaryPoints(
                latestDate,
                ("hs_left_shoulder", candles[leftShoulderIndex].High),
                ("hs_head", headHigh),
                ("hs_right_shoulder", candles[rightShoulderIndex].High),
                ("neckline", neckline));

            if (currentPrice < neckline)
            {
                return BuildBreakdownConfirmedState(
                    currentPrice, neckline, targetPrice, invalidationPrice,
                    leftShoulderIndex, headIndex, rightShoulderIndex,
                    structuralPoints, hasVolumeExpansion);
            }

            return BuildFormingState(
                currentPrice, neckline, targetPrice, invalidationPrice,
                leftShoulderIndex, headIndex, rightShoulderIndex,
                structuralPoints, hasVolumeExpansion);
        }

        // Triple boucle imbriquée en ordre chronologique croissant (épaule gauche la plus ancienne
        // d'abord) : combinatoire en O(nombre de pivots^3), acceptable ici car le nombre de pivots
        // hauts détectés sur une fenêtre d'analyse reste petit (quelques dizaines au plus). Retourne
        // le PREMIER triplet valide rencontré, donc celui dont l'épaule gauche est la plus ancienne
        // parmi les combinaisons satisfaisant tous les critères — pas nécessairement le plus symétrique
        // ni le plus proche du présent.
        private static (int LeftShoulderIndex, int HeadIndex, int RightShoulderIndex, decimal Neckline)?
            FindHeadAndShouldersTriplet(IReadOnlyList<TickerCandle> candles, List<int> pivotHighIndices)
        {
            if (pivotHighIndices.Count < 3)
            {
                return null;
            }

            for (var lsIdx = 0; lsIdx < pivotHighIndices.Count - 2; lsIdx++)
            {
                for (var hIdx = lsIdx + 1; hIdx < pivotHighIndices.Count - 1; hIdx++)
                {
                    for (var rsIdx = hIdx + 1; rsIdx < pivotHighIndices.Count; rsIdx++)
                    {
                        var leftShoulderIndex = pivotHighIndices[lsIdx];
                        var headIndex = pivotHighIndices[hIdx];
                        var rightShoulderIndex = pivotHighIndices[rsIdx];

                        var leftShoulderHigh = candles[leftShoulderIndex].High;
                        var headHigh = candles[headIndex].High;
                        var rightShoulderHigh = candles[rightShoulderIndex].High;

                        if (!IsHeadDominant(headHigh, leftShoulderHigh, rightShoulderHigh))
                        {
                            continue;
                        }

                        var neckline = ComputeNeckline(candles, leftShoulderIndex, headIndex, rightShoulderIndex);
                        var figureHeight = headHigh - neckline;

                        if (figureHeight <= 0m)
                        {
                            continue;
                        }

                        var avgShoulder = (leftShoulderHigh + rightShoulderHigh) / 2m;
                        var headDepthRatio = (headHigh - avgShoulder) / figureHeight;

                        if (headDepthRatio < PatternThresholds.HsMinHeadDepthRatio)
                        {
                            continue;
                        }

                        if (!AreShouldersSymmetric(leftShoulderIndex, headIndex, rightShoulderIndex))
                        {
                            continue;
                        }

                        return (leftShoulderIndex, headIndex, rightShoulderIndex, neckline);
                    }
                }
            }

            return null;
        }

        private static bool IsHeadDominant(decimal headHigh, decimal leftShoulderHigh, decimal rightShoulderHigh)
        {
            return headHigh > leftShoulderHigh && headHigh > rightShoulderHigh;
        }

        // Symétrie temporelle (nombre de bougies), pas symétrie de prix : le ratio largeur
        // gauche/droite doit rester dans [1/HsShoulderSymmetryRatio, HsShoulderSymmetryRatio] — les
        // deux épaules doivent prendre un temps comparable à se former, sans quoi la figure ressemble
        // davantage à un mouvement asymétrique quelconque qu'à un vrai tête-épaules.
        private static bool AreShouldersSymmetric(int leftShoulderIndex, int headIndex, int rightShoulderIndex)
        {
            var leftWidth = headIndex - leftShoulderIndex;
            var rightWidth = rightShoulderIndex - headIndex;

            if (leftWidth <= 0 || rightWidth <= 0)
            {
                return false;
            }

            var ratio = (decimal)leftWidth / rightWidth;
            var symmetryBound = PatternThresholds.HsShoulderSymmetryRatio;
            return ratio >= (1m / symmetryBound) && ratio <= symmetryBound;
        }

        // La neckline est la moyenne des deux creux (gauche et droit) qui séparent les trois sommets,
        // et non le plus bas des deux : une neckline légèrement inclinée entre les deux creux serait
        // plus fidèle au tracé technique classique, mais cette moyenne reste une approximation stable
        // et simple à interpréter côté affichage.
        private static decimal ComputeNeckline(
            IReadOnlyList<TickerCandle> candles,
            int leftShoulderIndex,
            int headIndex,
            int rightShoulderIndex)
        {
            var leftTroughLow = FindMinLowBetween(candles, leftShoulderIndex, headIndex);
            var rightTroughLow = FindMinLowBetween(candles, headIndex, rightShoulderIndex);
            return (leftTroughLow + rightTroughLow) / 2m;
        }

        private static decimal FindMinLowBetween(IReadOnlyList<TickerCandle> candles, int fromIndex, int toIndex)
        {
            var minLow = candles[fromIndex].Low;

            for (var i = fromIndex + 1; i <= toIndex; i++)
            {
                if (candles[i].Low < minLow)
                {
                    minLow = candles[i].Low;
                }
            }

            return minLow;
        }

        private static bool HasRecentVolumeExpansion(IReadOnlyList<TickerCandle> candles)
            => PatternTechnicals.IsVolumeExpanding(candles);

        // Mêmes paliers de confiance par phase que le double top/bottom (0.80 confirmé, 0.65 en
        // formation, 0.50 par défaut) — conserve une échelle de confiance cohérente entre les
        // patterns de retournement, indépendamment de leur complexité géométrique respective.
        private static decimal BuildConfidence(string phase, bool hasVolumeExpansion)
        {
            var confidence = phase switch
            {
                "hs_breakdown_confirmed" => 0.80m,
                "hs_forming" => 0.65m,
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
                PhaseCode = "hs_not_confirmed",
                PhaseLabel = "Tete-Epaules non identifie",
                Status = PatternStatus.Forming,
                IsCompatible = false,
                StatusReason = "Aucun triplet tete-epaules valide n'a ete detecte dans l'historique disponible.",
                ValidationReason = "La structure tete-epaules n'est pas constituee.",
                InvalidationReason = "Aucune invalidation exploitable tant que la figure n'est pas constituee.",
                Confidence = BuildConfidence("hs_not_confirmed", false),
                CurrentPrice = currentPrice,
                ScoreReasons = ["Pas de triplet sommet-tete-sommet avec tete dominante et epaules symetriques detecte."]
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
                "Triplet tete-epaules identifie : tete dominante et epaules symetriques.",
                "La neckline n'est pas encore cassee : attente de confirmation baissiere."
            };

            if (hasVolumeExpansion)
            {
                scoreReasons.Add("Volume recent superieur a la moyenne : signal d'intensification des vendeurs.");
            }

            return new ReversalPatternAnalysisState
            {
                PhaseCode = "hs_forming",
                PhaseLabel = "Tete-Epaules en formation",
                Status = PatternStatus.Monitoring,
                IsCompatible = true,
                StatusReason = "Le triplet tete-epaules est identifie. La cassure sous la neckline reste a confirmer.",
                ValidationReason = "Une cloture sous la neckline est necessaire pour confirmer le retournement baissier.",
                InvalidationReason = "Un retour du prix au-dessus de la tete invaliderait la figure.",
                Confidence = BuildConfidence("hs_forming", hasVolumeExpansion),
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

        private static ReversalPatternAnalysisState BuildBreakdownConfirmedState(
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
                "Triplet tete-epaules confirme avec tete dominante et epaules symetriques.",
                "Le prix cloture sous la neckline : retournement baissier confirme.",
                "Cible mesuree = neckline - hauteur de la figure (measured move baissier)."
            };

            if (hasVolumeExpansion)
            {
                scoreReasons.Add("Volume recent superieur a la moyenne : cassure soutenue par les volumes.");
            }

            return new ReversalPatternAnalysisState
            {
                PhaseCode = "hs_breakdown_confirmed",
                PhaseLabel = "Cassure baissiere confirmee",
                Status = PatternStatus.Confirmed,
                IsCompatible = true,
                IsValidated = true,
                StatusReason = "Le prix cloture sous la neckline apres formation d'un tete-epaules valide.",
                ValidationReason = "La cassure sous la neckline confirme le retournement baissier de la figure tete-epaules.",
                ValidationRuleCode = "HS_NECKLINE_BREAKDOWN_CLOSE",
                InvalidationReason = "Le scenario reste actif tant que le prix ne repasse pas au-dessus de la tete.",
                Confidence = BuildConfidence("hs_breakdown_confirmed", hasVolumeExpansion),
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
