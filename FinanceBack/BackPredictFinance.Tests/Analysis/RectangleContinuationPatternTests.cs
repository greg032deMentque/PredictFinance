using BackPredictFinance.Common.AnalysisV1;
using BackPredictFinance.Common.MarketData;
using BackPredictFinance.Patterns.Abstractions;
using BackPredictFinance.Patterns.Definitions;

namespace BackPredictFinance.Tests.Analysis;

public sealed class RectangleContinuationPatternTests
{
    private static AnalysisRequest BuildRequest()
    {
        return new AnalysisRequest
        {
            Instrument = new Instrument { Symbol = "TEST" },
            HistoryStartDate = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(-6)),
            HistoryEndDate = DateOnly.FromDateTime(DateTime.UtcNow),
            CandleInterval = "1d"
        };
    }

    private static TickerCandle Flat(decimal price, decimal volume = 1_000m)
    {
        return new TickerCandle
        {
            Date = DateTime.UtcNow,
            Open = price,
            High = price,
            Low = price,
            Close = price,
            Volume = volume
        };
    }

    private static TickerCandle Candle(decimal open, decimal high, decimal low, decimal close, decimal volume = 1_000m)
    {
        return new TickerCandle
        {
            Date = DateTime.UtcNow,
            Open = open,
            High = high,
            Low = low,
            Close = close,
            Volume = volume
        };
    }

    // Rectangle de reference : 24 bougies identiques touchant les 2 bornes a chaque bougie
    // (support=95, resistance=105, close=100) — bornes parfaitement plates, touches maximales.
    private static List<TickerCandle> BuildRectangleRangeWindow()
    {
        var candles = new List<TickerCandle>();
        for (var i = 0; i < 24; i++)
        {
            candles.Add(Candle(open: 100m, high: 105m, low: 95m, close: 100m));
        }

        return candles;
    }

    // Rampe stricte de 20 bougies (Flat) : mouvement net (>> seuil ATR) dans la direction demandee.
    private static List<TickerCandle> BuildPriorTrendWindow(decimal from, decimal to)
    {
        var candles = new List<TickerCandle>();
        for (var i = 0; i < 20; i++)
        {
            var price = from + (to - from) * i / 19m;
            candles.Add(Flat(decimal.Round(price, 4)));
        }

        return candles;
    }

    [Fact]
    public async Task Analyze_TrendingCandles_NoRepeatedTouches_ReturnsStructureNotConfirmed()
    {
        // 44 bougies en rampe stricte : chaque bougie fixe un nouveau plus haut, aucune borne
        // n'est touchee 2 fois -> pas de rectangle.
        var candles = new List<TickerCandle>();
        for (var i = 0; i < 44; i++)
        {
            candles.Add(Flat(50m + i * 5m));
        }

        var definition = new RectangleContinuationAnalysisPatternDefinition(new FakePatternMarketDataProvider(candles));
        var artifact = await definition.ExecuteAsync(BuildRequest());

        var pattern = artifact.Patterns[0];
        Assert.Equal("structure_not_confirmed", pattern.Phase);
        Assert.False(pattern.ContractAssessment.Detection.IsCompatible);
    }

    [Fact]
    public async Task Analyze_RectangleWithPriorUptrend_NoBreakout_ReturnsBullishForming()
    {
        var candles = BuildPriorTrendWindow(from: 70m, to: 89m)
            .Concat(BuildRectangleRangeWindow())
            .ToList();

        var definition = new RectangleContinuationAnalysisPatternDefinition(new FakePatternMarketDataProvider(candles));
        var artifact = await definition.ExecuteAsync(BuildRequest());

        var pattern = artifact.Patterns[0];
        Assert.Equal("bullish_rectangle_forming", pattern.Phase);
        Assert.True(pattern.ContractAssessment.Detection.IsCompatible);
        Assert.False(pattern.ContractAssessment.Validation.State == "VALIDATED");
    }

    [Fact]
    public async Task Analyze_RectangleWithPriorDowntrend_NoBreakout_ReturnsBearishForming()
    {
        var candles = BuildPriorTrendWindow(from: 130m, to: 111m)
            .Concat(BuildRectangleRangeWindow())
            .ToList();

        var definition = new RectangleContinuationAnalysisPatternDefinition(new FakePatternMarketDataProvider(candles));
        var artifact = await definition.ExecuteAsync(BuildRequest());

        var pattern = artifact.Patterns[0];
        Assert.Equal("bearish_rectangle_forming", pattern.Phase);
        Assert.True(pattern.ContractAssessment.Detection.IsCompatible);
    }

    [Fact]
    public async Task Analyze_RectangleWithoutPriorTrend_ReturnsNeutral()
    {
        var flatPriorWindow = Enumerable.Range(0, 20).Select(_ => Flat(100m)).ToList();
        var candles = flatPriorWindow
            .Concat(BuildRectangleRangeWindow())
            .ToList();

        var definition = new RectangleContinuationAnalysisPatternDefinition(new FakePatternMarketDataProvider(candles));
        var artifact = await definition.ExecuteAsync(BuildRequest());

        var pattern = artifact.Patterns[0];
        Assert.Equal("neutral_rectangle_without_prior_trend", pattern.Phase);
        Assert.False(pattern.ContractAssessment.Detection.IsCompatible);
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
