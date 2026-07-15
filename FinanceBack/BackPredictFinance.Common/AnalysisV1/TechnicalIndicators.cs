using BackPredictFinance.Common.enums;
using BackPredictFinance.Common.MarketData;

namespace BackPredictFinance.Common.AnalysisV1
{
    public static class TechnicalIndicators
    {
        public const int DefaultRsiPeriod = 14;
        public const int DefaultMacdFast = 12;
        public const int DefaultMacdSlow = 26;
        public const int DefaultMacdSignal = 9;
        public const int DefaultVolumeAvgPeriod = 20;
        public const decimal VolumeStrongMultiplier = 1.5m;
        public const decimal VolumeWeakMultiplier = 0.7m;
        public const decimal RegimeLowVolThreshold = 0.008m;
        public const decimal RegimeHighVolThreshold = 0.022m;
        public const decimal RsiOversoldThreshold = 30m;
        public const decimal RsiOverboughtThreshold = 70m;
        public const decimal AtrStopMultiplier = 1.5m;
        public const decimal DefaultRiskBudgetPct = 0.01m;

        public static decimal ComputeRsi(IReadOnlyList<TickerCandle> candles, int period = DefaultRsiPeriod)
        {
            if (candles == null || candles.Count < 2)
            {
                return 50m;
            }

            var effectivePeriod = Math.Min(period, candles.Count - 1);
            if (effectivePeriod < 1)
            {
                return 50m;
            }

            var gains = new decimal[candles.Count - 1];
            var losses = new decimal[candles.Count - 1];

            for (var i = 1; i < candles.Count; i++)
            {
                var delta = candles[i].Close - candles[i - 1].Close;
                if (delta > 0m)
                {
                    gains[i - 1] = delta;
                }
                else
                {
                    losses[i - 1] = Math.Abs(delta);
                }
            }

            var warmupGain = 0m;
            var warmupLoss = 0m;
            for (var i = 0; i < effectivePeriod; i++)
            {
                warmupGain += gains[i];
                warmupLoss += losses[i];
            }

            var avgGain = warmupGain / effectivePeriod;
            var avgLoss = warmupLoss / effectivePeriod;

            for (var i = effectivePeriod; i < gains.Length; i++)
            {
                avgGain = ((avgGain * (effectivePeriod - 1)) + gains[i]) / effectivePeriod;
                avgLoss = ((avgLoss * (effectivePeriod - 1)) + losses[i]) / effectivePeriod;
            }

            if (avgLoss == 0m)
            {
                return 100m;
            }

            var rs = avgGain / avgLoss;
            return 100m - (100m / (1m + rs));
        }

        public static RsiZone ClassifyRsiZone(decimal rsi)
        {
            if (rsi <= RsiOversoldThreshold)
            {
                return RsiZone.Oversold;
            }

            if (rsi >= RsiOverboughtThreshold)
            {
                return RsiZone.Overbought;
            }

            return RsiZone.Neutral;
        }

        public static (decimal Macd, decimal Signal, decimal Histogram, MacdCross Cross) ComputeMacd(
            IReadOnlyList<TickerCandle> candles,
            int fast = DefaultMacdFast,
            int slow = DefaultMacdSlow,
            int signal = DefaultMacdSignal)
        {
            if (candles == null || candles.Count < slow + signal)
            {
                return (0m, 0m, 0m, MacdCross.None);
            }

            var closes = candles.Select(c => c.Close).ToList();

            var emaFastSeries = ComputeEmaSeries(closes, fast);
            var emaSlowSeries = ComputeEmaSeries(closes, slow);

            var startIndex = slow - 1;
            var macdLine = new List<decimal>(closes.Count - startIndex);
            for (var i = startIndex; i < closes.Count; i++)
            {
                macdLine.Add(emaFastSeries[i] - emaSlowSeries[i]);
            }

            var signalLine = ComputeEmaSeries(macdLine, signal);

            var currentMacd = macdLine[^1];
            var currentSignal = signalLine[^1];
            var currentHistogram = currentMacd - currentSignal;

            var cross = MacdCross.None;
            if (macdLine.Count >= 2 && signalLine.Count >= 2)
            {
                var prevMacd = macdLine[^2];
                var prevSignal = signalLine[^2];
                if (prevMacd <= prevSignal && currentMacd > currentSignal)
                {
                    cross = MacdCross.BullishCross;
                }
                else if (prevMacd >= prevSignal && currentMacd < currentSignal)
                {
                    cross = MacdCross.BearishCross;
                }
            }

            return (
                Math.Round(currentMacd, 6),
                Math.Round(currentSignal, 6),
                Math.Round(currentHistogram, 6),
                cross);
        }

        public static (MarketRegime Regime, bool RegimeWarning) ComputeMarketRegime(IReadOnlyList<TickerCandle> candles, int period = DefaultVolumeAvgPeriod)
        {
            if (candles == null || candles.Count < 2)
            {
                return (MarketRegime.NormalVolatility, false);
            }

            var tail = candles.Count > period ? candles.Skip(candles.Count - period).ToList() : candles.ToList();

            var logReturns = new List<decimal>(tail.Count - 1);
            for (var i = 1; i < tail.Count; i++)
            {
                if (tail[i - 1].Close <= 0m || tail[i].Close <= 0m)
                {
                    continue;
                }

                logReturns.Add((decimal)Math.Log((double)(tail[i].Close / tail[i - 1].Close)));
            }

            if (logReturns.Count < 2)
            {
                return (MarketRegime.NormalVolatility, false);
            }

            var mean = logReturns.Average();
            var variance = logReturns.Sum(r => (r - mean) * (r - mean)) / (logReturns.Count - 1);
            var stdDev = (decimal)Math.Sqrt((double)variance);

            MarketRegime regime;
            if (stdDev < RegimeLowVolThreshold)
            {
                regime = MarketRegime.LowVolatility;
            }
            else if (stdDev > RegimeHighVolThreshold)
            {
                regime = MarketRegime.HighVolatility;
            }
            else
            {
                regime = MarketRegime.NormalVolatility;
            }

            return (regime, regime == MarketRegime.HighVolatility);
        }

        public static (decimal VolumeRatio, VolumeConfirmation Confirmation) ComputeVolumeConfirmation(
            IReadOnlyList<TickerCandle> candles,
            int avgPeriod = DefaultVolumeAvgPeriod)
        {
            if (candles == null || candles.Count < 2)
            {
                return (1m, VolumeConfirmation.Neutral);
            }

            var breakoutVolume = candles[^1].Volume;

            var windowSize = Math.Min(avgPeriod, candles.Count - 1);
            var avgWindow = candles.Skip(candles.Count - 1 - windowSize).Take(windowSize).ToList();

            if (avgWindow.Count == 0)
            {
                return (1m, VolumeConfirmation.Neutral);
            }

            var avg20 = avgWindow.Average(c => c.Volume);
            if (avg20 <= 0m)
            {
                return (1m, VolumeConfirmation.Neutral);
            }

            var ratio = Math.Round(breakoutVolume / avg20, 4);

            VolumeConfirmation confirmation;
            if (ratio >= VolumeStrongMultiplier)
            {
                confirmation = VolumeConfirmation.Strong;
            }
            else if (ratio <= VolumeWeakMultiplier)
            {
                confirmation = VolumeConfirmation.Weak;
            }
            else
            {
                confirmation = VolumeConfirmation.Neutral;
            }

            return (ratio, confirmation);
        }

        public static decimal ComputeVolumeAvg20(IReadOnlyList<TickerCandle> candles, int avgPeriod = DefaultVolumeAvgPeriod)
        {
            if (candles == null || candles.Count == 0)
            {
                return 0m;
            }

            var windowSize = Math.Min(avgPeriod, candles.Count);
            return candles.Skip(candles.Count - windowSize).Average(c => c.Volume);
        }

        public static decimal AverageTrueRange(IReadOnlyList<TickerCandle> candles, int period = 14)
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
            for (var i = 1; i < candles.Count; i++)
            {
                var high = candles[i].High;
                var low = candles[i].Low;
                var previousClose = candles[i - 1].Close;
                trueRanges[i] = Math.Max(
                    high - low,
                    Math.Max(Math.Abs(high - previousClose), Math.Abs(low - previousClose)));
            }

            var effectivePeriod = Math.Min(period < 1 ? 1 : period, trueRanges.Length);
            decimal atr = 0m;
            for (var i = 0; i < effectivePeriod; i++)
            {
                atr += trueRanges[i];
            }

            atr /= effectivePeriod;

            for (var i = effectivePeriod; i < trueRanges.Length; i++)
            {
                atr = ((atr * (effectivePeriod - 1)) + trueRanges[i]) / effectivePeriod;
            }

            return atr;
        }

        public static decimal ComputeAtrBasedStopLoss(decimal currentPrice, decimal atrValue, decimal multiplier = AtrStopMultiplier)
        {
            return currentPrice - (multiplier * atrValue);
        }

        public static (decimal Target1, decimal Target2) ComputeAtrBasedTargets(decimal entryPrice, decimal stopLoss)
        {
            var rDistance = Math.Abs(entryPrice - stopLoss);
            return (entryPrice + rDistance, entryPrice + (2m * rDistance));
        }

        public static decimal? ComputePositionSizePct(
            decimal entryPrice,
            decimal stopLossPrice,
            decimal riskBudgetPct = DefaultRiskBudgetPct)
        {
            if (entryPrice <= 0m || stopLossPrice <= 0m)
            {
                return null;
            }

            var stopDistancePct = Math.Abs(entryPrice - stopLossPrice) / entryPrice;
            if (stopDistancePct <= 0m)
            {
                return null;
            }

            return Math.Round(riskBudgetPct / stopDistancePct, 4);
        }

        private static List<decimal> ComputeEmaSeries(IReadOnlyList<decimal> values, int period)
        {
            var result = new List<decimal>(values.Count);
            if (values.Count == 0 || period < 1)
            {
                return result;
            }

            var effectivePeriod = Math.Min(period, values.Count);
            var multiplier = 2m / (effectivePeriod + 1m);

            decimal ema = 0m;
            for (var i = 0; i < effectivePeriod; i++)
            {
                ema += values[i];
            }

            ema /= effectivePeriod;

            for (var i = 0; i < effectivePeriod - 1; i++)
            {
                result.Add(ema);
            }

            result.Add(ema);

            for (var i = effectivePeriod; i < values.Count; i++)
            {
                ema = ((values[i] - ema) * multiplier) + ema;
                result.Add(ema);
            }

            return result;
        }
    }
}
