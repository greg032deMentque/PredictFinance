using BackPredictFinance.Common.AnalysisV1;
using BackPredictFinance.Common.MarketData;
using BackPredictFinance.Patterns.Abstractions;
using BackPredictFinance.Patterns.Definitions;

namespace BackPredictFinance.Tests.Analysis;

public sealed class BearFlagContinuationPatternTests
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

    private static List<TickerCandle> BuildPadding(int count = 18, decimal price = 100m)
    {
        return Enumerable.Range(0, count).Select(_ => Flat(price)).ToList();
    }

    private static List<TickerCandle> BuildPole(decimal from, decimal to, int steps = 12)
    {
        var candles = new List<TickerCandle>();
        for (var i = 0; i < steps; i++)
        {
            var price = from + (to - from) * i / (steps - 1);
            candles.Add(Flat(decimal.Round(price, 4)));
        }

        return candles;
    }

    private static List<TickerCandle> BuildTightFlag(decimal open, decimal high, decimal low, decimal close, int count = 10)
    {
        return Enumerable.Range(0, count).Select(_ => Candle(open, high, low, close)).ToList();
    }

    [Fact]
    public async Task Analyze_WeakPoleDrop_ReturnsStructureNotConfirmed()
    {
        var candles = BuildPadding()
            .Concat(BuildPole(from: 100m, to: 98m))
            .Concat(BuildTightFlag(open: 98m, high: 98.5m, low: 97.5m, close: 98m))
            .ToList();

        var definition = new BearFlagContinuationAnalysisPatternDefinition(new FakePatternMarketDataProvider(candles));
        var artifact = await definition.ExecuteAsync(BuildRequest());

        var pattern = artifact.Patterns[0];
        Assert.Equal("flag_structure_not_confirmed", pattern.Phase);
        Assert.False(pattern.ContractAssessment.Detection.IsCompatible);
    }

    [Fact]
    public async Task Analyze_DeepRebound_ReturnsStructureNotConfirmed()
    {
        var candles = BuildPadding()
            .Concat(BuildPole(from: 100m, to: 85m))
            .Concat(BuildTightFlag(open: 99.5m, high: 100m, low: 99m, close: 99.5m))
            .ToList();

        var definition = new BearFlagContinuationAnalysisPatternDefinition(new FakePatternMarketDataProvider(candles));
        var artifact = await definition.ExecuteAsync(BuildRequest());

        var pattern = artifact.Patterns[0];
        Assert.Equal("flag_structure_not_confirmed", pattern.Phase);
        Assert.False(pattern.ContractAssessment.Detection.IsCompatible);
    }

    [Fact]
    public async Task Analyze_CleanPoleDropAndTightFlag_ReturnsBearFlagForming()
    {
        var candles = BuildPadding()
            .Concat(BuildPole(from: 100m, to: 87m))
            .Concat(BuildTightFlag(open: 89m, high: 90m, low: 88m, close: 89m))
            .ToList();

        var definition = new BearFlagContinuationAnalysisPatternDefinition(new FakePatternMarketDataProvider(candles));
        var artifact = await definition.ExecuteAsync(BuildRequest());

        var pattern = artifact.Patterns[0];
        Assert.Equal("bear_flag_forming", pattern.Phase);
        Assert.True(pattern.ContractAssessment.Detection.IsCompatible);
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
