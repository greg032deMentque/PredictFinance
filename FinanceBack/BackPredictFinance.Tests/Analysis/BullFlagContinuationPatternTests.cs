using BackPredictFinance.Common.AnalysisV1;
using BackPredictFinance.Common.MarketData;
using BackPredictFinance.Patterns.Abstractions;
using BackPredictFinance.Patterns.Definitions;

namespace BackPredictFinance.Tests.Analysis;

// Les deux derniers tests prouvent la non-regression du fix "lot-breakout-fix" : avant correction,
// flagResistance/flagSupport etaient calcules sur une fenetre incluant la bougie testee elle-meme,
// rendant bullish_breakout_confirmed et flag_support_broken mathematiquement inatteignables
// (High >= Close >= Low interdit a une bougie de depasser un extremum qui l'inclut).
public sealed class BullFlagContinuationPatternTests
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

    // Serie de 40 bougies (minimum requis) : 18 bougies de padding, 12 bougies de pole haussier net
    // (100 -> 115, +15 %), 9 bougies de flag resserre (oscillation 112/114), puis 1 bougie testee
    // dont le Close est fourni par l'appelant.
    private static List<TickerCandle> BuildBullFlagSeries(decimal testedClose)
    {
        var candles = new List<TickerCandle>();

        for (var i = 0; i < 18; i++)
        {
            candles.Add(Flat(100m));
        }

        for (var i = 0; i < 12; i++)
        {
            candles.Add(Flat(decimal.Round(100m + (15m * i / 11m), 4)));
        }

        decimal[] flag = [112m, 113m, 114m, 113m];
        for (var i = 0; i < 9; i++)
        {
            candles.Add(Flat(flag[i % flag.Length]));
        }

        candles.Add(Flat(testedClose));

        return candles;
    }

    [Fact]
    public async Task Analyze_WeakPole_ReturnsStructureNotConfirmed()
    {
        var candles = BuildPadding()
            .Concat(BuildPole(from: 100m, to: 102m))
            .Concat(BuildTightFlag(open: 102m, high: 102.5m, low: 101.5m, close: 102m))
            .ToList();

        var definition = new BullFlagContinuationAnalysisPatternDefinition(new FakePatternMarketDataProvider(candles));
        var artifact = await definition.ExecuteAsync(BuildRequest());

        var pattern = artifact.Patterns[0];
        Assert.Equal("flag_structure_not_confirmed", pattern.Phase);
        Assert.False(pattern.ContractAssessment.Detection.IsCompatible);
    }

    [Fact]
    public async Task Analyze_DeepRetracement_ReturnsStructureNotConfirmed()
    {
        var candles = BuildPadding()
            .Concat(BuildPole(from: 100m, to: 115m))
            .Concat(BuildTightFlag(open: 101m, high: 101.5m, low: 100.5m, close: 101m))
            .ToList();

        var definition = new BullFlagContinuationAnalysisPatternDefinition(new FakePatternMarketDataProvider(candles));
        var artifact = await definition.ExecuteAsync(BuildRequest());

        var pattern = artifact.Patterns[0];
        Assert.Equal("flag_structure_not_confirmed", pattern.Phase);
        Assert.False(pattern.ContractAssessment.Detection.IsCompatible);
    }

    [Fact]
    public async Task Analyze_CleanPoleAndTightFlag_ReturnsBullFlagForming()
    {
        var candles = BuildPadding()
            .Concat(BuildPole(from: 100m, to: 113m))
            .Concat(BuildTightFlag(open: 111m, high: 112m, low: 110m, close: 111m))
            .ToList();

        var definition = new BullFlagContinuationAnalysisPatternDefinition(new FakePatternMarketDataProvider(candles));
        var artifact = await definition.ExecuteAsync(BuildRequest());

        var pattern = artifact.Patterns[0];
        Assert.Equal("bull_flag_forming", pattern.Phase);
        Assert.True(pattern.ContractAssessment.Detection.IsCompatible);
        Assert.Equal("NOT_VALIDATED", pattern.ContractAssessment.Validation.State);
    }

    [Fact]
    public async Task Analyze_CloseAboveFlagResistanceEstablishedBeforeTestedCandle_ReturnsBullishBreakoutConfirmed()
    {
        var candles = BuildBullFlagSeries(testedClose: 120m);
        var definition = new BullFlagContinuationAnalysisPatternDefinition(new FakePatternMarketDataProvider(candles));

        var artifact = await definition.ExecuteAsync(BuildRequest());

        var pattern = artifact.Patterns[0];
        Assert.Equal("bullish_breakout_confirmed", pattern.Phase);
        Assert.True(pattern.ContractAssessment.Detection.IsCompatible);
        Assert.Equal("VALIDATED", pattern.ContractAssessment.Validation.State);
        Assert.NotNull(pattern.TargetPrice);
    }

    [Fact]
    public async Task Analyze_CloseBelowFlagSupportEstablishedBeforeTestedCandle_ReturnsFlagSupportBroken()
    {
        var candles = BuildBullFlagSeries(testedClose: 105m);
        var definition = new BullFlagContinuationAnalysisPatternDefinition(new FakePatternMarketDataProvider(candles));

        var artifact = await definition.ExecuteAsync(BuildRequest());

        var pattern = artifact.Patterns[0];
        Assert.Equal("flag_support_broken", pattern.Phase);
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
