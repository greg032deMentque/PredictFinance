using BackPredictFinance.Common.MarketData;

namespace BackPredictFinance.Patterns.Common
{
    public static class SupportResistanceDetector
    {
        /// <summary>
        /// Detecte les zones de support/resistance en regroupant les pivots (hauts et bas) par
        /// "bin" de prix dont la largeur est calee sur l'ATR : deux pivots separes de moins d'un
        /// ATR sont consideres comme touchant la meme zone plutot que deux niveaux distincts.
        /// Seules les zones ayant au moins <paramref name="minTouches"/> touches sont conservees,
        /// puis le resultat est plafonne aux zones les plus touchees (overlay purement indicatif,
        /// pas un signal de detection de pattern).
        /// </summary>
        public static IReadOnlyList<SupportResistanceZone> Detect(
            IReadOnlyList<TickerCandle> candles,
            int minTouches = PatternThresholds.SrMinTouches)
        {
            if (candles == null || candles.Count == 0)
            {
                return [];
            }

            var atr = PatternTechnicals.AverageTrueRange(candles);
            // Taille de bin = ATR : deux prix distants de moins d'un ATR tombent dans le meme bin.
            var binSize = ResolveBinSize(atr, candles);

            var highIndices = PatternTechnicals.FindPivotHighs(candles, PatternThresholds.PivotHalfWindow);
            var lowIndices = PatternTechnicals.FindPivotLows(candles, PatternThresholds.PivotHalfWindow);

            var highPrices = highIndices.Select(i => candles[i].High).ToList();
            var lowPrices = lowIndices.Select(i => candles[i].Low).ToList();

            var binHighCounts = new Dictionary<long, int>();
            var binLowCounts = new Dictionary<long, int>();

            foreach (var price in highPrices)
            {
                var binIndex = (long)Math.Floor(price / binSize);
                binHighCounts.TryGetValue(binIndex, out var current);
                binHighCounts[binIndex] = current + 1;
            }

            foreach (var price in lowPrices)
            {
                var binIndex = (long)Math.Floor(price / binSize);
                binLowCounts.TryGetValue(binIndex, out var current);
                binLowCounts[binIndex] = current + 1;
            }

            var allBins = binHighCounts.Keys.Union(binLowCounts.Keys).ToHashSet();

            var zones = new List<SupportResistanceZone>();

            foreach (var binIndex in allBins)
            {
                binHighCounts.TryGetValue(binIndex, out var highCount);
                binLowCounts.TryGetValue(binIndex, out var lowCount);
                var totalTouches = highCount + lowCount;

                if (totalTouches < minTouches)
                {
                    continue;
                }

                var priceLow = binIndex * binSize;
                var priceHigh = (binIndex + 1) * binSize;
                var priceMid = (priceLow + priceHigh) / 2m;

                var zoneType = ResolveZoneType(highCount, lowCount);
                // Force normalisee [0-1] : atteint son maximum a partir de SrStrengthMaxTouches
                // touches au-dela du minimum requis, plafonnee a 1.
                var strength = Math.Min(1m, (totalTouches - minTouches + 1m) / PatternThresholds.SrStrengthMaxTouches);

                zones.Add(new SupportResistanceZone
                {
                    PriceLow = priceLow,
                    PriceHigh = priceHigh,
                    PriceMid = priceMid,
                    TouchCount = totalTouches,
                    ZoneType = zoneType,
                    Strength = strength
                });
            }

            // Selection des zones les plus significatives (par nombre de touches), puis retri par
            // prix croissant pour un affichage lisible de bas en haut.
            return zones
                .OrderByDescending(z => z.TouchCount)
                .Take(PatternThresholds.SrMaxZones)
                .OrderBy(z => z.PriceMid)
                .ToList();
        }

        private static decimal ResolveBinSize(decimal atr, IReadOnlyList<TickerCandle> candles)
        {
            if (atr > 0m)
            {
                return atr;
            }

            // Fallback si l'ATR est nul (historique trop court ou serie plate) : fraction du prix
            // median, avec un plancher a 1 pour ne jamais diviser par une taille de bin nulle.
            var median = candles.Select(c => c.Close).OrderBy(p => p).ElementAt(candles.Count / 2);
            var fallback = Math.Abs(median) * PatternThresholds.SrAtrFallbackPriceFraction;
            return fallback > 0m ? fallback : 1m;
        }

        private static string ResolveZoneType(int highCount, int lowCount)
        {
            if (highCount > 0 && lowCount == 0)
            {
                return "resistance";
            }

            if (lowCount > 0 && highCount == 0)
            {
                return "support";
            }

            return "both";
        }
    }
}
