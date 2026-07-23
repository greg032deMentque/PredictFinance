using BackPredictFinance.Common.AnalysisV1;
using BackPredictFinance.Common.enums;
using BackPredictFinance.Common.MarketData;
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
        private const int BbPeriod = 20;

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
            var candles = ToTickerCandles(histories);

            var rsi = ComputeRsi(candles);
            var macd = ComputeMacd(candles);
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

        private static List<TickerCandle> ToTickerCandles(List<PriceHistory> histories)
        {
            var candles = new List<TickerCandle>(histories.Count);
            foreach (var history in histories)
            {
                candles.Add(new TickerCandle
                {
                    Date = history.RetrievedAtUtc,
                    Open = history.Price,
                    High = history.Price,
                    Low = history.Price,
                    Close = history.Price,
                    Volume = history.Volume ?? 0m
                });
            }

            return candles;
        }

        private static RsiIndicatorViewModel? ComputeRsi(List<TickerCandle> candles)
        {
            if (candles.Count <= TechnicalIndicators.DefaultRsiPeriod) return null;

            var rsi = Math.Round(TechnicalIndicators.ComputeRsi(candles, TechnicalIndicators.DefaultRsiPeriod), 2);
            var zone = TechnicalIndicators.ClassifyRsiZone(rsi);
            var signal = zone switch
            {
                RsiZone.Oversold => "Survente",
                RsiZone.Overbought => "Surachat",
                _ => "Neutre"
            };

            return new RsiIndicatorViewModel { Value = rsi, Signal = signal };
        }

        private static MacdIndicatorViewModel? ComputeMacd(List<TickerCandle> candles)
        {
            var minRequired = TechnicalIndicators.DefaultMacdSlow + TechnicalIndicators.DefaultMacdSignal;
            if (candles.Count < minRequired) return null;

            var (macd, signal, histogram, _) = TechnicalIndicators.ComputeMacd(
                candles,
                TechnicalIndicators.DefaultMacdFast,
                TechnicalIndicators.DefaultMacdSlow,
                TechnicalIndicators.DefaultMacdSignal);

            return new MacdIndicatorViewModel
            {
                Line = Math.Round(macd, 4),
                SignalLine = Math.Round(signal, 4),
                Histogram = Math.Round(histogram, 4),
                Trend = macd >= signal ? "Haussier" : "Baissier"
            };
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
