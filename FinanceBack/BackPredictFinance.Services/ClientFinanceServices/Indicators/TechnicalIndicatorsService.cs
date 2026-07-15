using BackPredictFinance.Datas.Entities;
using BackPredictFinance.ViewModels.ClientFinanceViewModels.Indicators;
using Microsoft.EntityFrameworkCore;

namespace BackPredictFinance.Services.ClientFinanceServices.Indicators
{
    public interface ITechnicalIndicatorsService
    {
        Task<TechnicalIndicatorsViewModel?> GetIndicatorsAsync(string symbol, CancellationToken ct);
    }

    public sealed class TechnicalIndicatorsService : BaseService, ITechnicalIndicatorsService
    {
        private const int MaxDataPoints = 250;
        private const int RsiPeriod = 14;
        private const int MacdFast = 12;
        private const int MacdSlow = 26;
        private const int MacdSignalPeriod = 9;
        private const int BbPeriod = 20;
        private const int MacdMinRequired = MacdSlow + MacdSignalPeriod - 1;

        public TechnicalIndicatorsService(IServiceProvider serviceProvider) : base(serviceProvider) { }

        public async Task<TechnicalIndicatorsViewModel?> GetIndicatorsAsync(string symbol, CancellationToken ct)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(symbol);

            var normalized = symbol.Trim().ToUpperInvariant();

            var asset = await _financeDbContext.Set<Asset>()
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.Symbol == normalized, ct);

            if (asset == null) return null;

            var histories = await _financeDbContext.Set<PriceHistory>()
                .AsNoTracking()
                .Where(p => p.AssetId == asset.Id)
                .OrderByDescending(p => p.RetrievedAtUtc)
                .Take(MaxDataPoints)
                .ToListAsync(ct);

            histories = [.. histories.OrderBy(p => p.RetrievedAtUtc)];

            if (histories.Count < 2)
                return new TechnicalIndicatorsViewModel { Symbol = normalized, DataPointsUsed = histories.Count };

            var prices = histories.Select(p => p.Price).ToList();
            var currentPrice = prices[^1];

            var rsi = ComputeRsi(prices);
            var macd = ComputeMacd(prices);
            var bb = ComputeBollingerBands(prices, currentPrice);
            var ma = ComputeMovingAverages(prices, currentPrice);

            return new TechnicalIndicatorsViewModel
            {
                Symbol = normalized,
                ComputedAtUtc = DateTime.UtcNow,
                DataPointsUsed = histories.Count,
                Rsi = rsi,
                Macd = macd,
                BollingerBands = bb,
                MovingAverages = ma,
                Obv = ComputeObv(histories),
                Synthesis = ComputeSynthesis(rsi, macd, bb, ma)
            };
        }

        private static RsiIndicatorViewModel? ComputeRsi(List<decimal> prices)
        {
            if (prices.Count <= RsiPeriod) return null;

            var changes = new List<decimal>(prices.Count - 1);
            for (int i = 1; i < prices.Count; i++)
                changes.Add(prices[i] - prices[i - 1]);

            var seed = changes.Take(RsiPeriod).ToList();
            var avgGain = seed.Where(c => c > 0).Sum() / RsiPeriod;
            var avgLoss = seed.Where(c => c < 0).Select(c => Math.Abs(c)).Sum() / RsiPeriod;

            for (int i = RsiPeriod; i < changes.Count; i++)
            {
                var gain = changes[i] > 0 ? changes[i] : 0m;
                var loss = changes[i] < 0 ? Math.Abs(changes[i]) : 0m;
                avgGain = (avgGain * (RsiPeriod - 1) + gain) / RsiPeriod;
                avgLoss = (avgLoss * (RsiPeriod - 1) + loss) / RsiPeriod;
            }

            if (avgLoss == 0m) return new RsiIndicatorViewModel { Value = 100m, Signal = "Surachat" };

            var rsi = Math.Round(100m - 100m / (1m + avgGain / avgLoss), 2);
            var signal = rsi >= 70m ? "Surachat" : rsi <= 30m ? "Survente" : "Neutre";

            return new RsiIndicatorViewModel { Value = rsi, Signal = signal };
        }

        private static MacdIndicatorViewModel? ComputeMacd(List<decimal> prices)
        {
            if (prices.Count < MacdMinRequired) return null;

            var ema12 = ComputeEmaSequence(prices, MacdFast);
            var ema26 = ComputeEmaSequence(prices, MacdSlow);

            var macdLine = new List<decimal>(prices.Count - MacdSlow + 1);
            for (int i = MacdSlow - 1; i < prices.Count; i++)
                macdLine.Add(ema12[i] - ema26[i]);

            var signalEma = ComputeEmaSequence(macdLine, MacdSignalPeriod);

            var lastMacd = macdLine[^1];
            var lastSignal = signalEma[^1];

            return new MacdIndicatorViewModel
            {
                Line = Math.Round(lastMacd, 4),
                SignalLine = Math.Round(lastSignal, 4),
                Histogram = Math.Round(lastMacd - lastSignal, 4),
                Trend = lastMacd >= lastSignal ? "Haussier" : "Baissier"
            };
        }

        private static List<decimal> ComputeEmaSequence(List<decimal> source, int period)
        {
            var ema = new decimal[source.Count];
            ema[period - 1] = source.Take(period).Average();
            var multiplier = 2m / (period + 1);
            for (int i = period; i < source.Count; i++)
                ema[i] = (source[i] - ema[i - 1]) * multiplier + ema[i - 1];
            return [.. ema];
        }

        private static BollingerBandsViewModel? ComputeBollingerBands(List<decimal> prices, decimal currentPrice)
        {
            if (prices.Count < BbPeriod) return null;

            var window = prices.Skip(prices.Count - BbPeriod).ToList();
            var middle = window.Average();
            var variance = window.Average(p => (p - middle) * (p - middle));
            var stdDev = (decimal)Math.Sqrt((double)variance);

            var upper = middle + 2m * stdDev;
            var lower = middle - 2m * stdDev;

            var position = currentPrice > upper ? "Au-dessus de la bande haute"
                : currentPrice < lower ? "En dessous de la bande basse"
                : "Dans les bandes";

            return new BollingerBandsViewModel
            {
                Upper = Math.Round(upper, 4),
                Middle = Math.Round(middle, 4),
                Lower = Math.Round(lower, 4),
                CurrentPrice = currentPrice,
                Position = position
            };
        }

        private static MovingAveragesViewModel ComputeMovingAverages(List<decimal> prices, decimal currentPrice)
        {
            return new MovingAveragesViewModel
            {
                Ma20 = prices.Count >= 20 ? Math.Round(prices.Skip(prices.Count - 20).Average(), 4) : null,
                Ma50 = prices.Count >= 50 ? Math.Round(prices.Skip(prices.Count - 50).Average(), 4) : null,
                Ma200 = prices.Count >= 200 ? Math.Round(prices.Skip(prices.Count - 200).Average(), 4) : null,
                CurrentPrice = currentPrice
            };
        }

        private static decimal? ComputeObv(List<PriceHistory> histories)
        {
            if (histories.Count(p => p.Volume.HasValue) < histories.Count / 2) return null;

            decimal obv = 0m;
            for (int i = 1; i < histories.Count; i++)
            {
                if (!histories[i].Volume.HasValue) continue;
                if (histories[i].Price > histories[i - 1].Price) obv += histories[i].Volume!.Value;
                else if (histories[i].Price < histories[i - 1].Price) obv -= histories[i].Volume!.Value;
            }

            return Math.Round(obv, 0);
        }

        private static IndicatorSynthesisViewModel? ComputeSynthesis(
            RsiIndicatorViewModel? rsi,
            MacdIndicatorViewModel? macd,
            BollingerBandsViewModel? bb,
            MovingAveragesViewModel ma)
        {
            int bullish = 0, bearish = 0, total = 0;

            if (rsi != null)
            {
                total++;
                if (rsi.Signal == "Survente") bullish++;
                else if (rsi.Signal == "Surachat") bearish++;
            }

            if (macd != null)
            {
                total++;
                if (macd.Trend == "Haussier") bullish++;
                else bearish++;
            }

            if (bb != null)
            {
                total++;
                if (bb.Position == "En dessous de la bande basse") bullish++;
                else if (bb.Position == "Au-dessus de la bande haute") bearish++;
            }

            if (ma.Ma20.HasValue || ma.Ma50.HasValue || ma.Ma200.HasValue)
            {
                total++;
                var maScore = ComputeMaScore(ma);
                if (maScore > 0) bullish++;
                else if (maScore < 0) bearish++;
            }

            if (total == 0) return null;

            var score = bullish - bearish;
            var label = score >= 3 ? "Fortement haussier"
                : score == 2 ? "Haussier"
                : score == 1 ? "Légèrement haussier"
                : score == 0 ? "Mixte"
                : score == -1 ? "Légèrement baissier"
                : score == -2 ? "Baissier"
                : "Fortement baissier";

            return new IndicatorSynthesisViewModel
            {
                Label = label,
                BullishSignals = bullish,
                BearishSignals = bearish,
                TotalSignals = total
            };
        }

        private static int ComputeMaScore(MovingAveragesViewModel ma)
        {
            int votes = 0;
            if (ma.Ma20.HasValue) votes += ma.CurrentPrice >= ma.Ma20.Value ? 1 : -1;
            if (ma.Ma50.HasValue) votes += ma.CurrentPrice >= ma.Ma50.Value ? 1 : -1;
            if (ma.Ma200.HasValue) votes += ma.CurrentPrice >= ma.Ma200.Value ? 1 : -1;
            if (votes > 0) return 1;
            if (votes < 0) return -1;
            return 0;
        }
    }
}
