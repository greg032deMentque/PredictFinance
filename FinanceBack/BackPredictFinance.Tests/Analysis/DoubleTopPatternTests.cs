using BackPredictFinance.Common.AnalysisV1;
using BackPredictFinance.Common.MarketData;
using BackPredictFinance.Patterns.Abstractions;
using BackPredictFinance.Patterns.Common;
using BackPredictFinance.Patterns.Definitions;

namespace BackPredictFinance.Tests.Analysis;

public sealed class DoubleTopPatternTests
{
    private static DoubleTopReversalPatternDefinition BuildSut(IReadOnlyList<TickerCandle> candles)
    {
        return new DoubleTopReversalPatternDefinition(new FakePatternMarketDataProvider(candles));
    }

    private static AnalysisRequest BuildRequest()
    {
        return new AnalysisRequest
        {
            Instrument = new Instrument { Symbol = "TEST" },
            HistoryStartDate = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(-12)),
            HistoryEndDate = DateOnly.FromDateTime(DateTime.UtcNow),
            CandleInterval = "1d"
        };
    }

    private static TickerCandle Candle(decimal high, decimal low, decimal close, decimal volume = 1_000m)
    {
        return new TickerCandle
        {
            Date = DateTime.UtcNow,
            Open = close,
            High = high,
            Low = low,
            Close = close,
            Volume = volume
        };
    }

    // Série de référence strictement croissante : FindPivotHighs ne trouve aucun pivot car
    // chaque candidat a un voisin droit plus élevé. Aucune paire double top possible.
    private static List<TickerCandle> BuildStrictlyIncreasingCandles(int count)
    {
        var candles = new List<TickerCandle>();
        for (var i = 0; i < count; i++)
        {
            var high = 50m + i * 1m;
            candles.Add(Candle(high: high, low: high - 1m, close: high - 0.5m));
        }

        return candles;
    }

    private static List<TickerCandle> BuildDoubleTopSeries(decimal peakHigh, decimal necklineLow, decimal finalClose)
    {
        var candles = new List<TickerCandle>();

        // Padding croissant initial : aucun pivot ne se forme sur une montée stricte
        for (var i = 0; i < 30; i++)
        {
            var h = necklineLow - 30m + i * 1m;
            candles.Add(Candle(high: h, low: h - 1m, close: h - 0.5m));
        }

        // Montée vers premier sommet
        AddRamp(candles, from: necklineLow, to: peakHigh, steps: 8);
        candles.Add(Candle(high: peakHigh, low: peakHigh - 1m, close: peakHigh - 0.5m));
        // Descente vers neckline
        AddRamp(candles, from: peakHigh - 0.5m, to: necklineLow, steps: 8);
        // Montée vers second sommet (~1% du premier — dans la tolérance 6%)
        var peakHigh2 = peakHigh * 1.01m;
        AddRamp(candles, from: necklineLow, to: peakHigh2, steps: 8);
        candles.Add(Candle(high: peakHigh2, low: peakHigh2 - 1m, close: peakHigh2 - 0.5m));
        // Descente finale
        AddRamp(candles, from: peakHigh2 - 0.5m, to: finalClose + 2m, steps: 5);
        candles.Add(Candle(high: finalClose + 1m, low: finalClose - 1m, close: finalClose));

        return candles;
    }

    [Fact]
    public async Task Analyze_TwoEqualHighs_WithBreakdownBelowNeckline_ReturnsConfirmedPhase()
    {
        // double top classique : deux sommets à ~120, neckline ~100, close final sous la neckline
        var candles = BuildDoubleTopSeries(peakHigh: 120m, necklineLow: 100m, finalClose: 88m);

        var artifact = await BuildSut(candles).ExecuteAsync(BuildRequest());
        var phase = artifact.Patterns[0].Phase;

        Assert.Equal("double_top_breakout_confirmed", phase);
        Assert.True(artifact.Patterns[0].NecklinePrice.HasValue);
        Assert.True(artifact.Patterns[0].TargetPrice.HasValue);
    }

    [Fact]
    public async Task Analyze_TwoHighsTooFarApart_PriceAboveNeckline_ReturnNotConfirmed()
    {
        // Série strictement croissante : FindPivotHighs ne retourne aucun pivot → not_confirmed garanti
        var candles = BuildStrictlyIncreasingCandles(65);

        var artifact = await BuildSut(candles).ExecuteAsync(BuildRequest());
        var phase = artifact.Patterns[0].Phase;

        Assert.Equal("double_top_not_confirmed", phase);
        Assert.False(artifact.Patterns[0].ContractAssessment.Detection.IsCompatible);
    }

    [Fact]
    public async Task Analyze_NoLookAhead_LastNBarsNotUsedAsPivots()
    {
        // Série croissante puis PivotHalfWindow hauts en fin.
        // Ces dernières bougies ne peuvent PAS être confirmées comme pivots hauts
        // car i+n >= count → look-ahead exclu.
        var candles = BuildStrictlyIncreasingCandles(60);
        var topHigh = 500m;

        for (var i = 0; i < PatternThresholds.PivotHalfWindow; i++)
        {
            candles.Add(Candle(high: topHigh + i, low: topHigh + i - 2m, close: topHigh + i - 1m));
        }

        var artifact = await BuildSut(candles).ExecuteAsync(BuildRequest());
        var phase = artifact.Patterns[0].Phase;

        Assert.NotEqual("double_top_breakout_confirmed", phase);
        Assert.NotEqual("double_top_forming", phase);
    }

    [Fact]
    public async Task Analyze_BreakdownConfirmed_TargetPriceSetToMeasuredMove()
    {
        var candles = BuildDoubleTopSeries(peakHigh: 120m, necklineLow: 100m, finalClose: 88m);

        var artifact = await BuildSut(candles).ExecuteAsync(BuildRequest());
        var pattern = artifact.Patterns[0];

        Assert.True(pattern.TargetPrice.HasValue);
        Assert.True(pattern.NecklinePrice.HasValue);
        Assert.True(pattern.TargetPrice.Value < pattern.NecklinePrice.Value);
    }

    private static void AddRamp(List<TickerCandle> candles, decimal from, decimal to, int steps)
    {
        for (var i = 1; i <= steps; i++)
        {
            var price = from + (to - from) * i / steps;
            var rounded = decimal.Round(price, 4);
            candles.Add(Candle(high: rounded + 0.5m, low: rounded - 0.5m, close: rounded));
        }
    }

    private sealed class FakePatternMarketDataProvider : IPatternMarketDataProvider
    {
        private readonly IReadOnlyList<TickerCandle> _candles;

        public FakePatternMarketDataProvider(IReadOnlyList<TickerCandle> candles)
        {
            _candles = candles;
        }

        public Task<TickerTimeSeriesResponse> GetTimeSeriesAsync(string symbol, string interval, int outputSize, CancellationToken ct = default)
        {
            return Task.FromResult(new TickerTimeSeriesResponse
            {
                Symbol = symbol,
                Interval = interval,
                OutputSize = _candles.Count,
                Candles = _candles.ToList()
            });
        }
    }
}
