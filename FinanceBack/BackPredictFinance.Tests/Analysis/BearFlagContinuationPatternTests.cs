using BackPredictFinance.Common.AnalysisV1;
using BackPredictFinance.Common.MarketData;
using BackPredictFinance.Patterns.Abstractions;
using BackPredictFinance.Patterns.Definitions;

namespace BackPredictFinance.Tests.Analysis;

// Les deux derniers tests prouvent la non-regression du fix "lot-breakout-fix", miroir baissier de
// BullFlagContinuationPatternTests : avant correction, flagResistance/flagSupport etaient calcules
// sur une fenetre incluant la bougie testee elle-meme, rendant bearish_breakout_confirmed et
// flag_resistance_broken mathematiquement inatteignables (High >= Close >= Low interdit a une
// bougie de depasser un extremum qui l'inclut).
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

    // Serie de 40 bougies (minimum requis) : 18 bougies de padding, 12 bougies de pole baissier net
    // (115 -> 100, -13 %), 9 bougies de flag resserre (rebond 100/102), puis 1 bougie testee dont le
    // Close est fourni par l'appelant.
    private static List<TickerCandle> BuildBearFlagSeries(decimal testedClose)
    {
        var candles = new List<TickerCandle>();

        for (var i = 0; i < 18; i++)
        {
            candles.Add(Flat(115m));
        }

        for (var i = 0; i < 12; i++)
        {
            candles.Add(Flat(decimal.Round(115m - (15m * i / 11m), 4)));
        }

        decimal[] flag = [100m, 101m, 102m, 101m];
        for (var i = 0; i < 9; i++)
        {
            candles.Add(Flat(flag[i % flag.Length]));
        }

        candles.Add(Flat(testedClose));

        return candles;
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
        Assert.Equal("NOT_VALIDATED", pattern.ContractAssessment.Validation.State);
    }

    [Fact]
    public async Task Analyze_CloseBelowFlagSupportEstablishedBeforeTestedCandle_ReturnsBearishBreakoutConfirmed()
    {
        var candles = BuildBearFlagSeries(testedClose: 90m);
        var definition = new BearFlagContinuationAnalysisPatternDefinition(new FakePatternMarketDataProvider(candles));

        var artifact = await definition.ExecuteAsync(BuildRequest());

        var pattern = artifact.Patterns[0];
        Assert.Equal("bearish_breakout_confirmed", pattern.Phase);
        Assert.True(pattern.ContractAssessment.Detection.IsCompatible);
        Assert.Equal("VALIDATED", pattern.ContractAssessment.Validation.State);
        Assert.NotNull(pattern.TargetPrice);
    }

    [Fact]
    public async Task Analyze_CloseAboveFlagResistanceEstablishedBeforeTestedCandle_ReturnsFlagResistanceBroken()
    {
        var candles = BuildBearFlagSeries(testedClose: 110m);
        var definition = new BearFlagContinuationAnalysisPatternDefinition(new FakePatternMarketDataProvider(candles));

        var artifact = await definition.ExecuteAsync(BuildRequest());

        var pattern = artifact.Patterns[0];
        Assert.Equal("flag_resistance_broken", pattern.Phase);
        Assert.Equal("INVALIDATED", pattern.ContractAssessment.Invalidation.State);
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
