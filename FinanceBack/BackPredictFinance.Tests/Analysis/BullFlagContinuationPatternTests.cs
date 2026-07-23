using BackPredictFinance.Common.AnalysisV1;
using BackPredictFinance.Common.MarketData;
using BackPredictFinance.Patterns.Abstractions;
using BackPredictFinance.Patterns.Definitions;

namespace BackPredictFinance.Tests.Analysis;

// Preuve de non-regression du fix "lot-breakout-fix" : avant correction, flagResistance/flagSupport
// etaient calcules sur une fenetre incluant la bougie testee elle-meme, rendant
// bullish_breakout_confirmed et flag_support_broken mathematiquement inatteignables
// (High >= Close >= Low interdit a une bougie de depasser un extremum qui l'inclut).
public sealed class BullFlagContinuationPatternTests
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

    // Pole plat (aucune impulsion) : poleGainPct reste sous le seuil minimal, la structure de bull
    // flag n'est jamais constituee, quel que soit le flag. 40 bougies = MinimumRequiredCandles exact.
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

    [Fact]
    public async Task Analyze_CloseInsideFlag_ReturnsBullFlagForming()
    {
        var candles = BuildBullFlagSeries(testedClose: 113m);
        var definition = new BullFlagContinuationAnalysisPatternDefinition(new FakePatternMarketDataProvider(candles));

        var artifact = await definition.ExecuteAsync(BuildRequest());

        var pattern = artifact.Patterns[0];
        Assert.Equal("bull_flag_forming", pattern.Phase);
        Assert.True(pattern.ContractAssessment.Detection.IsCompatible);
        Assert.Equal("NOT_VALIDATED", pattern.ContractAssessment.Validation.State);
    }

    [Fact]
    public async Task Analyze_NoPoleImpulse_ReturnsFlagStructureNotConfirmed()
    {
        var candles = BuildSeriesWithoutPoleImpulse();
        var definition = new BullFlagContinuationAnalysisPatternDefinition(new FakePatternMarketDataProvider(candles));

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
