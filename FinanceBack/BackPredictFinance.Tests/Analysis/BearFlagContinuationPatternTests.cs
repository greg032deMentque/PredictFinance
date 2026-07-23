using BackPredictFinance.Common.AnalysisV1;
using BackPredictFinance.Common.MarketData;
using BackPredictFinance.Patterns.Abstractions;
using BackPredictFinance.Patterns.Definitions;

namespace BackPredictFinance.Tests.Analysis;

// Preuve de non-regression du fix "lot-breakout-fix", miroir baissier de BullFlagContinuationPatternTests :
// avant correction, flagResistance/flagSupport etaient calcules sur une fenetre incluant la bougie
// testee elle-meme, rendant bearish_breakout_confirmed et flag_resistance_broken mathematiquement
// inatteignables (High >= Close >= Low interdit a une bougie de depasser un extremum qui l'inclut).
public sealed class BearFlagContinuationPatternTests
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

    // Pole plat (aucune impulsion) : poleDropPct reste sous le seuil minimal en valeur absolue, la
    // structure de bear flag n'est jamais constituee. 40 bougies = MinimumRequiredCandles exact.
    private static List<TickerCandle> BuildSeriesWithoutPoleImpulse()
    {
        var candles = new List<TickerCandle>();

        for (var i = 0; i < 40; i++)
        {
            candles.Add(Flat(100m));
        }

        return candles;
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

    [Fact]
    public async Task Analyze_CloseInsideFlag_ReturnsBearFlagForming()
    {
        var candles = BuildBearFlagSeries(testedClose: 101m);
        var definition = new BearFlagContinuationAnalysisPatternDefinition(new FakePatternMarketDataProvider(candles));

        var artifact = await definition.ExecuteAsync(BuildRequest());

        var pattern = artifact.Patterns[0];
        Assert.Equal("bear_flag_forming", pattern.Phase);
        Assert.True(pattern.ContractAssessment.Detection.IsCompatible);
        Assert.Equal("NOT_VALIDATED", pattern.ContractAssessment.Validation.State);
    }

    [Fact]
    public async Task Analyze_NoPoleImpulse_ReturnsFlagStructureNotConfirmed()
    {
        var candles = BuildSeriesWithoutPoleImpulse();
        var definition = new BearFlagContinuationAnalysisPatternDefinition(new FakePatternMarketDataProvider(candles));

        var artifact = await definition.ExecuteAsync(BuildRequest());

        var pattern = artifact.Patterns[0];
        Assert.Equal("flag_structure_not_confirmed", pattern.Phase);
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
