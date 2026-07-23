using BackPredictFinance.Common.AnalysisV1;
using BackPredictFinance.Common.enums;
using BackPredictFinance.Common.MarketData;
using BackPredictFinance.Patterns.Abstractions;
using BackPredictFinance.Patterns.Common;

namespace BackPredictFinance.Patterns.Definitions
{
    /// <summary>
    /// Détecte un double top : deux sommets (pivots hauts) de hauteur quasi égale séparés par un
    /// creux intermédiaire suffisamment profond (la "neckline"), interprété comme un retournement
    /// baissier une fois la neckline cassée à la clôture. Le second sommet n'invalide pas mais ne
    /// confirme pas non plus tant que le prix ne clôture pas sous la neckline.
    /// </summary>
    public sealed class DoubleTopReversalPatternDefinition : ReversalPatternDefinitionBase
    {
        public DoubleTopReversalPatternDefinition(IPatternMarketDataProvider marketDataProvider)
            : base(marketDataProvider)
        {
        }

        public override string PatternId => PatternIds.DoubleTop;
        public override string ModelVersion => "analysis-v1-double-top@m4";
        public override int HistoryLookbackMonths => 12;
        protected override int MinimumRequiredCandles => 60;
        protected override string DisplayName => "Double top";
        protected override string PedagogicalDescription => "Deux sommets proches en hauteur separes par un creux intermediaire, baissier seulement apres cassure sous la neckline.";
        protected override decimal HistoricalReliability => BulkowskiReliability.DoubleTop;

        protected override ReversalPatternAnalysisState Analyze(AnalysisRequest request, IReadOnlyList<TickerCandle> candles)
        {
            if (candles.Count < MinimumRequiredCandles)
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
                    CurrentPrice = candles[^1].Close,
                    ScoreReasons = ["Le nombre minimal de bougies n'est pas atteint pour ce pattern."]
                };
            }

            var currentPrice = candles[^1].Close;
            var pivotHighIndices = PatternTechnicals.FindPivotHighs(candles, PatternThresholds.PivotHalfWindow);

            var peakPair = FindBestPeakPair(candles, pivotHighIndices);

            if (peakPair is null)
            {
                return new ReversalPatternAnalysisState
                {
                    PhaseCode = "double_top_not_confirmed",
                    PhaseLabel = "Aucun double sommet detecte",
                    Status = PatternStatus.Forming,
                    IsCompatible = false,
                    StatusReason = "Aucune paire de sommets egaux et suffisamment espaces n'a ete detectee.",
                    ValidationReason = "Deux sommets proches et un creux intermediaire suffisant sont requis pour constituer un double top.",
                    InvalidationReason = "Aucune invalidation exploitable sans structure identifiee.",
                    Confidence = BuildConfidence(false, false, false),
                    CurrentPrice = currentPrice,
                    ScoreReasons = ["Aucune paire de sommets valide trouvee dans l'historique disponible."]
                };
            }

            var (firstIndex, secondIndex, necklinePrice) = peakPair.Value;
            var high1 = candles[firstIndex].High;
            var high2 = candles[secondIndex].High;
#pragma warning disable S125 // Faux positif Sonar : prose explicative (le "pourquoi"), pas du code commenté.
            // Hauteur de la figure = distance entre la moyenne des deux sommets et la neckline ;
            // l'objectif de prix projette cette même distance sous la neckline (règle de mesure
            // classique de l'analyse technique pour les figures en double sommet/creux).
#pragma warning restore S125
            var figureHeight = (high1 + high2) / 2m - necklinePrice;
            var targetPrice = necklinePrice - figureHeight;
            var hasVolumeExpansion = PatternTechnicals.IsVolumeExpanding(candles);
            var breakdownConfirmed = currentPrice < necklinePrice;
            var confidence = BuildConfidence(true, breakdownConfirmed, hasVolumeExpansion);

            if (breakdownConfirmed)
            {
                return new ReversalPatternAnalysisState
                {
                    PhaseCode = "double_top_breakout_confirmed",
                    PhaseLabel = "Cassure sous la neckline confirmee",
                    Status = PatternStatus.Confirmed,
                    IsCompatible = true,
                    StatusReason = "Le prix cloture sous la neckline apres deux sommets proches en hauteur.",
                    IsValidated = true,
                    ValidationReason = "La cassure sous la neckline confirme le renversement baissier du double top.",
                    ValidationRuleCode = "DOUBLE_TOP_NECKLINE_BREAKDOWN_CLOSE",
                    InvalidationReason = "Le scenario reste actif tant que le prix ne repasse pas au-dessus du second sommet.",
                    Confidence = confidence,
                    CurrentPrice = currentPrice,
                    NecklinePrice = necklinePrice,
                    TargetPrice = targetPrice,
                    InvalidationPrice = Math.Max(high1, high2),
                    FirstPeakIndex = firstIndex,
                    SecondPeakIndex = secondIndex,
                    StructuralPoints = PatternTechnicals.BuildBoundaryPoints(
                        candles[secondIndex].Date,
                        ("first_top", high1),
                        ("second_top", high2),
                        ("neckline", necklinePrice),
                        ("breakdown", currentPrice)),
                    ScoreReasons = BuildScoreReasons(true, hasVolumeExpansion)
                };
            }

            return new ReversalPatternAnalysisState
            {
                PhaseCode = "double_top_forming",
                PhaseLabel = "Double top en formation",
                Status = PatternStatus.Monitoring,
                IsCompatible = true,
                StatusReason = "Deux sommets proches ont ete detectes avec un creux intermediaire suffisant, sans cassure sous la neckline pour l'instant.",
                ValidationReason = "Une cloture sous la neckline est necessaire pour confirmer le renversement baissier.",
                InvalidationReason = "Un depassement significatif du second sommet invaliderait la lecture de double top.",
                Confidence = confidence,
                CurrentPrice = currentPrice,
                NecklinePrice = necklinePrice,
                TargetPrice = targetPrice,
                InvalidationPrice = Math.Max(high1, high2),
                FirstPeakIndex = firstIndex,
                SecondPeakIndex = secondIndex,
                StructuralPoints = PatternTechnicals.BuildBoundaryPoints(
                    candles[secondIndex].Date,
                    ("first_top", high1),
                    ("second_top", high2),
                    ("neckline", necklinePrice)),
                ScoreReasons = BuildScoreReasons(false, hasVolumeExpansion)
            };
        }

        // Recherche du pivot le plus récent vers le plus ancien (boucle externe décroissante) : le
        // second sommet candidat est toujours le pivot le plus proche du présent. Pour chacun, on
        // cherche un premier sommet plus ancien de hauteur quasi égale (ArePricesEqual, tolérance
        // relative) en s'éloignant progressivement dans le passé, et on retourne la PREMIÈRE paire
        // valide trouvée — pas la "meilleure" au sens d'un score : le pivot le plus proche du présent
        // et le premier ancien compatible priment sur une paire théoriquement plus nette mais plus loin.
        // Une paire n'est retenue que si : l'écart minimal de bougies est respecté (évite deux pivots
        // du même mouvement de bruit), et le creux intermédiaire a suffisamment rebondi par rapport à
        // la hauteur de la figure (sinon ce n'est pas un vrai creux, juste une pause).
        private static (int FirstIndex, int SecondIndex, decimal NecklinePrice)? FindBestPeakPair(
            IReadOnlyList<TickerCandle> candles,
            List<int> pivotHighIndices)
        {
            if (pivotHighIndices.Count < 2)
            {
                return null;
            }

            for (var i = pivotHighIndices.Count - 1; i >= 1; i--)
            {
                var secondIndex = pivotHighIndices[i];
                var high2 = candles[secondIndex].High;

                for (var j = i - 1; j >= 0; j--)
                {
                    var firstIndex = pivotHighIndices[j];
                    var high1 = candles[firstIndex].High;

                    if (!PatternTechnicals.ArePricesEqual(high1, high2))
                    {
                        continue;
                    }

                    var barDistance = secondIndex - firstIndex;
                    if (barDistance < PatternThresholds.DoubleMinSeparationBars)
                    {
                        continue;
                    }

                    var necklinePrice = FindNecklinePrice(candles, firstIndex, secondIndex);
                    var avgPeaks = (high1 + high2) / 2m;
                    var figureHeight = avgPeaks - necklinePrice;

                    if (figureHeight <= 0m)
                    {
                        continue;
                    }

                    // Profondeur du creux intermediaire rapportee aux 2 extremes (les sommets), pas a
                    // figureHeight lui-meme : diviser par figureHeight rendait ce ratio structurellement
                    // ~1 (high1 ~ high2 par ArePricesEqual, donc figureHeight ~ high1 - necklinePrice),
                    // un filtre mort qui ne rejetait jamais rien. avgPeaks est independant de la
                    // profondeur du creux, donc ce ratio redevient un vrai pourcentage discriminant.
                    var intermediateRebound = figureHeight / avgPeaks;
                    if (intermediateRebound < PatternThresholds.DoubleMinIntermediateReboundPct)
                    {
                        continue;
                    }

                    return (firstIndex, secondIndex, necklinePrice);
                }
            }

            return null;
        }

        // La neckline est le plus bas close observé ENTRE les deux sommets (pas le plus bas Low) :
        // utiliser le close plutôt que le low évite qu'une simple mèche ponctuelle ne fixe un support
        // artificiellement bas.
        private static decimal FindNecklinePrice(IReadOnlyList<TickerCandle> candles, int firstIndex, int secondIndex)
        {
            var minClose = decimal.MaxValue;
            for (var k = firstIndex; k <= secondIndex; k++)
            {
                if (candles[k].Close < minClose)
                {
                    minClose = candles[k].Close;
                }
            }

            return minClose;
        }

        // La structure géométrique (deux sommets + creux valides) pèse plus lourd (0.35) que la
        // confirmation de cassure (0.15) : sans structure valide il n'y a pas de pattern du tout,
        // tandis que la cassure ne fait que déclencher un scénario déjà identifié. Le volume n'est
        // qu'un signal d'appoint (0.05).
        private static decimal BuildConfidence(bool hasStructure, bool breakdownConfirmed, bool hasVolumeExpansion)
        {
            var confidence = 0.15m;

            if (hasStructure)
            {
                confidence += 0.35m;
            }

            if (breakdownConfirmed)
            {
                confidence += 0.15m;
            }

            if (hasVolumeExpansion)
            {
                confidence += 0.05m;
            }

            return PatternTechnicals.Clamp01(confidence);
        }

        private static List<string> BuildScoreReasons(bool breakdownConfirmed, bool hasVolumeExpansion)
        {
            var reasons = new List<string>
            {
                "Deux sommets proches en hauteur detectes avec creux intermediaire suffisant."
            };

            if (breakdownConfirmed)
            {
                reasons.Add("La cassure sous la neckline confirme le renversement baissier.");
            }
            else
            {
                reasons.Add("La cassure sous la neckline reste le declencheur necessaire.");
            }

            if (hasVolumeExpansion)
            {
                reasons.Add("Volume expansif detecte : signal de momentum baissier supplementaire.");
            }

            return reasons;
        }
    }
}
