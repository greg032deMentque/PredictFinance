using BackPredictFinance.Common.AnalysisV1;
using BackPredictFinance.Common.MarketData;
using BackPredictFinance.Patterns.Abstractions;
using BackPredictFinance.Patterns.Definitions;

namespace BackPredictFinance.Tests.Analysis;

// Preuve de non-regression du fix "lot-breakout-fix" : avant correction, resistance/support etaient
// calcules sur une fenetre incluant la bougie testee elle-meme, rendant bullish_breakout_confirmed,
// bearish_breakout_confirmed et opposite_breakout_invalidated mathematiquement inatteignables
// (High >= Close >= Low interdit a une bougie de depasser un extremum qui l'inclut).
public sealed class RectangleContinuationPatternTests
{
    private static readonly DateOnly HistoryStart = new(2024, 1, 1);
    private static readonly DateOnly HistoryEnd = new(2024, 12, 31);

    private static AnalysisRequest BuildRequest()
    {
        return new AnalysisRequest
        {
            Instrument = new Instrument { Symbol = "TEST" },
            HistoryStartDate = HistoryStart,
            HistoryEndDate = HistoryEnd,
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

    // Serie de 44 bougies (minimum requis) : 20 bougies de tendance haussiere prealable (81..100,
    // +1/bougie), puis 23 bougies de rectangle 100/110 (oscillation periodique touchant les deux
    // bornes a plusieurs reprises), puis 1 bougie testee dont le Close est fourni par l'appelant.
    private static List<TickerCandle> BuildRectangleSeries(decimal testedClose)
    {
        var candles = new List<TickerCandle>();

        for (var price = 81m; price <= 100m; price++)
        {
            candles.Add(Flat(price));
        }

        decimal[] rectangle = [100m, 105m, 110m, 105m];
        for (var i = 0; i < 23; i++)
        {
            candles.Add(Flat(rectangle[i % rectangle.Length]));
        }

        candles.Add(Flat(testedClose));

        return candles;
    }

    // Meme tendance prealable, mais la fenetre de structure poursuit la rampe haussiere au lieu
    // d'osciller : aucune borne n'est touchee deux fois, le rectangle n'est jamais confirme.
    private static List<TickerCandle> BuildSeriesWithoutRectangleStructure()
    {
        var candles = new List<TickerCandle>();

        for (var price = 81m; price <= 100m; price++)
        {
            candles.Add(Flat(price));
        }

        for (var price = 101m; price <= 123m; price++)
        {
            candles.Add(Flat(price));
        }

        candles.Add(Flat(124m));

        return candles;
    }

    [Fact]
    public async Task Analyze_CloseAboveResistanceEstablishedBeforeTestedCandle_ReturnsBullishBreakoutConfirmed()
    {
        var candles = BuildRectangleSeries(testedClose: 115m);
        var definition = new RectangleContinuationAnalysisPatternDefinition(new FakePatternMarketDataProvider(candles));

        var artifact = await definition.ExecuteAsync(BuildRequest());

        var pattern = artifact.Patterns[0];
        Assert.Equal("bullish_breakout_confirmed", pattern.Phase);
        Assert.True(pattern.ContractAssessment.Detection.IsCompatible);
        Assert.Equal("VALIDATED", pattern.ContractAssessment.Validation.State);
        Assert.NotNull(pattern.TargetPrice);
    }

    [Fact]
    public async Task Analyze_CloseBelowSupportOppositeToPriorUptrend_ReturnsOppositeBreakoutInvalidated()
    {
        var candles = BuildRectangleSeries(testedClose: 95m);
        var definition = new RectangleContinuationAnalysisPatternDefinition(new FakePatternMarketDataProvider(candles));

        var artifact = await definition.ExecuteAsync(BuildRequest());

        var pattern = artifact.Patterns[0];
        Assert.Equal("opposite_breakout_invalidated", pattern.Phase);
        Assert.Equal("INVALIDATED", pattern.ContractAssessment.Invalidation.State);
        Assert.False(pattern.ContractAssessment.Detection.IsCompatible);
    }

    [Fact]
    public async Task Analyze_CloseInsideRange_ReturnsRectangleForming()
    {
        var candles = BuildRectangleSeries(testedClose: 107m);
        var definition = new RectangleContinuationAnalysisPatternDefinition(new FakePatternMarketDataProvider(candles));

        var artifact = await definition.ExecuteAsync(BuildRequest());

        var pattern = artifact.Patterns[0];
        Assert.Equal("bullish_rectangle_forming", pattern.Phase);
        Assert.True(pattern.ContractAssessment.Detection.IsCompatible);
        Assert.Equal("NOT_VALIDATED", pattern.ContractAssessment.Validation.State);
    }

    [Fact]
    public async Task Analyze_NoRepeatedBoundaryTouches_ReturnsStructureNotConfirmed()
    {
        var candles = BuildSeriesWithoutRectangleStructure();
        var definition = new RectangleContinuationAnalysisPatternDefinition(new FakePatternMarketDataProvider(candles));

        var artifact = await definition.ExecuteAsync(BuildRequest());

        var pattern = artifact.Patterns[0];
        Assert.Equal("structure_not_confirmed", pattern.Phase);
        Assert.False(pattern.ContractAssessment.Detection.IsCompatible);
    }

    private sealed class FakePatternMarketDataProvider : IPatternMarketDataProvider
    {
        private readonly List<TickerCandle> _candles;

        public FakePatternMarketDataProvider(List<TickerCandle> candles)
        {
            _candles = candles;
        }

        public Task<TickerTimeSeriesResponse> GetTimeSeriesAsync(string symbol, string interval, int outputSize, CancellationToken ct = default)
        {
            var response = new TickerTimeSeriesResponse
            {
                Symbol = symbol,
                Interval = interval,
                OutputSize = _candles.Count,
                Candles = _candles
            };

            return Task.FromResult(response);
        }
    }
}
