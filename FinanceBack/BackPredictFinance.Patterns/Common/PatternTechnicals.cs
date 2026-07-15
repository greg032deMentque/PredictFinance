using BackPredictFinance.Common.MarketData;
using BackPredictFinance.Common.AnalysisV1;

namespace BackPredictFinance.Patterns.Common
{
    internal static class PatternTechnicals
    {
        /// <summary>Periode standard de l'ATR de Wilder.</summary>
        public const int DefaultAtrPeriod = 14;

        /// <summary>
        /// Nombre de bougies a demander au fournisseur de donnees pour couvrir la fenetre
        /// d'analyse. Au moins <paramref name="minimumRequiredCandles"/> (seuil de detection du
        /// pattern), plafonne a 500 pour eviter une requete disproportionnee sur une fenetre
        /// calendaire tres large.
        /// </summary>
        public static int BuildRequestedCandleCount(DateOnly historyStartDate, DateOnly historyEndDate, int minimumRequiredCandles)
        {
            var calendarSpan = historyEndDate.DayNumber - historyStartDate.DayNumber + 1;
            return Math.Clamp(Math.Max(calendarSpan, minimumRequiredCandles), minimumRequiredCandles, 500);
        }

        public static decimal Clamp01(decimal value)
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

        public static decimal ComputeReturnPct(decimal startPrice, decimal endPrice)
        {
            if (startPrice <= 0m)
            {
                return 0m;
            }

            return (endPrice - startPrice) / startPrice;
        }

        /// <summary>
        /// Ecart relatif entre deux valeurs, normalise par la premiere valeur non nulle rencontree
        /// (a puis b en repli) pour rester utilisable meme quand l'une des deux vaut zero.
        /// </summary>
        public static decimal ComputeRelativeGap(decimal a, decimal b)
        {
            var denominator = Math.Abs(a) > 0m ? Math.Abs(a) : Math.Abs(b);
            if (denominator <= 0m)
            {
                return 0m;
            }

            return Math.Abs(a - b) / denominator;
        }

        public static decimal ComputeSlope(IReadOnlyList<decimal> values)
        {
            return ComputeLinearFit(values).Slope;
        }

        /// <summary>
        /// Regression lineaire ordinaire (moindres carres) d'une serie indexee 0..n-1.
        /// Retourne la pente et l'ordonnee a l'origine de la droite ajustee.
        /// L'ordonnee a l'origine est indispensable pour projeter correctement une borne : la
        /// droite de regression passe par la moyenne des points, pas par le premier point, donc
        /// l'ancrer sur la premiere bougie (qui peut etre une meche extreme) decale la projection.
        /// </summary>
        public static (decimal Slope, decimal Intercept) ComputeLinearFit(IReadOnlyList<decimal> values)
        {
            if (values == null || values.Count == 0)
            {
                return (0m, 0m);
            }

            if (values.Count == 1)
            {
                return (0m, values[0]);
            }

            var count = values.Count;
            var xMean = (count - 1) / 2d;
            var yMean = values.Average(value => (double)value);
            var numerator = 0d;
            var denominator = 0d;

            for (var index = 0; index < count; index++)
            {
                var x = index - xMean;
                numerator += x * ((double)values[index] - yMean);
                denominator += x * x;
            }

            if (denominator == 0d)
            {
                return (0m, Convert.ToDecimal(yMean));
            }

            var slope = numerator / denominator;
            var intercept = yMean - (slope * xMean);
            return (Convert.ToDecimal(slope), Convert.ToDecimal(intercept));
        }

        /// <summary>
        /// Average True Range selon Wilder (periode 14 par defaut).
        /// La True Range integre les gaps d'ouverture en comparant aussi a la cloture precedente :
        /// TR = max(H-L, |H - Close_prec|, |L - Close_prec|). Le lissage de Wilder applique ensuite
        /// une moyenne exponentielle modifiee. Retourne la valeur d'ATR la plus recente.
        /// </summary>
        public static decimal AverageTrueRange(IReadOnlyList<TickerCandle> candles, int period = DefaultAtrPeriod)
        {
            if (candles == null || candles.Count == 0)
            {
                return 0m;
            }

            if (candles.Count == 1)
            {
                return Math.Max(candles[0].High - candles[0].Low, 0m);
            }

            var trueRanges = new decimal[candles.Count];
            trueRanges[0] = Math.Max(candles[0].High - candles[0].Low, 0m);
            for (var index = 1; index < candles.Count; index++)
            {
                var high = candles[index].High;
                var low = candles[index].Low;
                var previousClose = candles[index - 1].Close;
                trueRanges[index] = Math.Max(
                    high - low,
                    Math.Max(Math.Abs(high - previousClose), Math.Abs(low - previousClose)));
            }

            var effectivePeriod = Math.Min(period < 1 ? 1 : period, trueRanges.Length);

            // Amorcage (warm-up) : moyenne simple des premieres True Ranges.
            decimal atr = 0m;
            for (var index = 0; index < effectivePeriod; index++)
            {
                atr += trueRanges[index];
            }

            atr /= effectivePeriod;

            // Lissage de Wilder sur le reste de la serie.
            for (var index = effectivePeriod; index < trueRanges.Length; index++)
            {
                atr = ((atr * (effectivePeriod - 1)) + trueRanges[index]) / effectivePeriod;
            }

            return atr;
        }

        /// <summary>
        /// Unite de volatilite utilisee pour normaliser les seuils : l'ATR de Wilder, plancher a une
        /// petite fraction du prix de reference pour eviter qu'une volatilite quasi nulle ne rende
        /// les seuils ATR degeneres.
        /// </summary>
        public static decimal VolatilityUnit(IReadOnlyList<TickerCandle> candles, decimal referencePrice)
        {
            var atr = AverageTrueRange(candles);
            var floor = Math.Abs(referencePrice) * PatternThresholds.AtrFloorPriceFraction;
            return Math.Max(atr, floor);
        }

        public static decimal AverageVolume(IReadOnlyList<TickerCandle> candles)
        {
            if (candles == null || candles.Count == 0)
            {
                return 0m;
            }

            return candles.Average(candle => candle.Volume);
        }

        public static decimal AverageClose(IReadOnlyList<TickerCandle> candles)
        {
            if (candles == null || candles.Count == 0)
            {
                return 0m;
            }

            return candles.Average(candle => candle.Close);
        }

        /// <summary>
        /// Construit les points structurels (bornes, sommets/creux, breakout...) affiches pour un
        /// pattern. Filtre les prix a zero ou negatifs et les types vides : un prix invalide
        /// (ex. figure pas encore assez formee pour avoir une valeur exploitable) est ecarte plutot
        /// que remonte tel quel a l'affichage.
        /// </summary>
        public static List<PatternStructuralPoint> BuildBoundaryPoints(DateTime timestamp, params (string PointType, decimal Price)[] points)
        {
            return points
                .Where(point => point.Price > 0m && !string.IsNullOrWhiteSpace(point.PointType))
                .Select(point => new PatternStructuralPoint
                {
                    PointType = point.PointType,
                    Timestamp = timestamp,
                    Price = decimal.Round(point.Price, 4)
                })
                .ToList();
        }

        public static IReadOnlyList<TickerCandle> Tail(IReadOnlyList<TickerCandle> candles, int count)
        {
            if (candles.Count <= count)
            {
                return candles.ToList();
            }

            return candles.Skip(candles.Count - count).ToList();
        }

        /// <summary>
        /// Retourne les indices des pivots hauts par fractale N-barres.
        /// Un pivot haut en i : candles[i].High est >= High de toutes les bougies dans [i-n, i+n].
        /// Seuls les pivots dont l'indice de confirmation (i+n) est strictement inferieur a
        /// candles.Count sont retournes — les N dernieres bougies ne sont jamais confirmees
        /// (prevention du look-ahead bias).
        /// </summary>
        public static List<int> FindPivotHighs(IReadOnlyList<TickerCandle> candles, int n = PatternThresholds.PivotHalfWindow)
        {
            var pivots = new List<int>();
            var safeN = Math.Max(1, n);

            for (var i = safeN; i < candles.Count - safeN; i++)
            {
                var candidateHigh = candles[i].High;
                var isPivot = true;

                for (var j = i - safeN; j <= i + safeN; j++)
                {
                    if (j == i) continue;
                    if (candles[j].High > candidateHigh)
                    {
                        isPivot = false;
                        break;
                    }
                }

                if (isPivot) pivots.Add(i);
            }

            return pivots;
        }

        /// <summary>
        /// Retourne les indices des pivots bas par fractale N-barres.
        /// Meme logique que FindPivotHighs avec Low. Les N dernieres bougies ne sont jamais
        /// retournees (prevention du look-ahead bias).
        /// </summary>
        public static List<int> FindPivotLows(IReadOnlyList<TickerCandle> candles, int n = PatternThresholds.PivotHalfWindow)
        {
            var pivots = new List<int>();
            var safeN = Math.Max(1, n);

            for (var i = safeN; i < candles.Count - safeN; i++)
            {
                var candidateLow = candles[i].Low;
                var isPivot = true;

                for (var j = i - safeN; j <= i + safeN; j++)
                {
                    if (j == i) continue;
                    if (candles[j].Low < candidateLow)
                    {
                        isPivot = false;
                        break;
                    }
                }

                if (isPivot) pivots.Add(i);
            }

            return pivots;
        }

        /// <summary>
        /// Retourne vrai si les deux prix sont consideres egaux a la tolerance pct pres.
        /// Utilise le plus grand des deux comme denominateur pour etre symetrique.
        /// </summary>
        public static bool ArePricesEqual(decimal a, decimal b, decimal tolerancePct = PatternThresholds.DoublePriceTolerance)
        {
            var denominator = Math.Max(Math.Abs(a), Math.Abs(b));
            if (denominator <= 0m) return true;
            return Math.Abs(a - b) / denominator <= tolerancePct;
        }

        /// <summary>
        /// Retourne vrai si le volume des <paramref name="recentBars"/> dernieres bougies
        /// est superieur au volume moyen de toute la serie.
        /// Facteur de confiance volume (+0.05) pour les patterns de retournement.
        /// </summary>
        public static bool IsVolumeExpanding(IReadOnlyList<TickerCandle> candles, int recentBars = PatternThresholds.PivotHalfWindow)
        {
            if (candles.Count <= recentBars) return false;
            var globalAvg = AverageVolume(candles);
            if (globalAvg <= 0m) return false;
            var recentSum = 0m;
            for (var i = candles.Count - recentBars; i < candles.Count; i++)
            {
                recentSum += candles[i].Volume;
            }
            return recentSum / recentBars > globalAvg;
        }
    }
}
