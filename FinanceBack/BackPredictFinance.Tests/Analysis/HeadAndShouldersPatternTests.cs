using BackPredictFinance.Common.AnalysisV1;
using BackPredictFinance.Common.MarketData;
using BackPredictFinance.Patterns.Abstractions;
using BackPredictFinance.Patterns.Common;
using BackPredictFinance.Patterns.Definitions;

namespace BackPredictFinance.Tests.Analysis;

public sealed class HeadAndShouldersPatternTests
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

    [Fact]
    public async Task Analyze_ValidTriplet_WithBreakdownBelowNeckline_ReturnsConfirmedPhase()
    {
        var candles = BuildHeadAndShouldersSeries(
            basePrice: 100m,
            shoulderHigh: 110m,
            headHigh: 120m,
            troughPrice: 100m,
            finalClose: 97m);

        var definition = new HeadAndShouldersReversalPatternDefinition(new FakePatternMarketDataProvider(candles));
        var artifact = await definition.ExecuteAsync(BuildRequest());

        var pattern = artifact.Patterns[0];
        Assert.Equal("hs_breakdown_confirmed", pattern.Phase);
        Assert.True(pattern.ContractAssessment.Detection.IsCompatible);
        Assert.Equal("VALIDATED", pattern.ContractAssessment.Validation.State);
        Assert.True(pattern.Confidence >= 0.80m);
    }

    [Fact]
    public async Task Analyze_ValidTriplet_NoPriceBreakdown_ReturnsFormingPhase()
    {
        var candles = BuildHeadAndShouldersSeries(
            basePrice: 100m,
            shoulderHigh: 110m,
            headHigh: 120m,
            troughPrice: 100m,
            finalClose: 103m);

        var definition = new HeadAndShouldersReversalPatternDefinition(new FakePatternMarketDataProvider(candles));
        var artifact = await definition.ExecuteAsync(BuildRequest());

        var pattern = artifact.Patterns[0];
        Assert.Equal("hs_forming", pattern.Phase);
        Assert.True(pattern.ContractAssessment.Detection.IsCompatible);
        Assert.Equal("NOT_VALIDATED", pattern.ContractAssessment.Validation.State);
        Assert.True(pattern.Confidence >= 0.65m);
    }

    [Fact]
    public async Task Analyze_HeadNotHighEnough_ReturnsNotConfirmed()
    {
        var candles = BuildFlatTripletSeries(
            shoulderHigh: 110m,
            headHigh: 110m,
            troughPrice: 100m,
            finalClose: 103m);

        var definition = new HeadAndShouldersReversalPatternDefinition(new FakePatternMarketDataProvider(candles));
        var artifact = await definition.ExecuteAsync(BuildRequest());

        var pattern = artifact.Patterns[0];
        Assert.Equal("hs_not_confirmed", pattern.Phase);
        Assert.False(pattern.ContractAssessment.Detection.IsCompatible);
    }

    [Fact]
    public async Task Analyze_NoLookAhead_LastNBarsNotUsedAsPivots()
    {
        var candles = BuildSeriesWithPeakAtEnd();

        var definition = new HeadAndShouldersReversalPatternDefinition(new FakePatternMarketDataProvider(candles));
        var artifact = await definition.ExecuteAsync(BuildRequest());

        var pattern = artifact.Patterns[0];
        Assert.NotEqual("hs_breakdown_confirmed", pattern.Phase);
        Assert.NotEqual("hs_forming", pattern.Phase);
    }

    // -----------------------------------------------------------------------
    // Helpers de construction de series synthetiques
    // -----------------------------------------------------------------------

    // Construit une serie M avec deux epaules a shoulderHigh et une tete a headHigh.
    // Creux intermediaires a troughPrice. Transitions strictement monotones.
    // Structure : base plate, montee vers ES gauche, descente, montee vers tete,
    // descente, montee vers ES droite, descente, finalClose.
    private static List<TickerCandle> BuildHeadAndShouldersSeries(
        decimal basePrice,
        decimal shoulderHigh,
        decimal headHigh,
        decimal troughPrice,
        decimal finalClose)
    {
        var candles = new List<TickerCandle>();

        for (var i = 0; i < 30; i++)
        {
            candles.Add(Flat(basePrice - i * 0.1m));
        }

        AddRamp(candles, from: basePrice, to: shoulderHigh, steps: 8);
        candles.Add(Flat(shoulderHigh));
        AddRamp(candles, from: shoulderHigh, to: troughPrice, steps: 8);
        candles.Add(Flat(troughPrice));
        AddRamp(candles, from: troughPrice, to: headHigh, steps: 8);
        candles.Add(Flat(headHigh));
        AddRamp(candles, from: headHigh, to: troughPrice, steps: 8);
        candles.Add(Flat(troughPrice));
        AddRamp(candles, from: troughPrice, to: shoulderHigh - 1m, steps: 8);
        candles.Add(Flat(shoulderHigh - 1m));
        AddRamp(candles, from: shoulderHigh - 1m, to: troughPrice + 1m, steps: 8);

        candles.Add(Flat(finalClose));

        return candles;
    }

    // Serie avec une tete au meme niveau que les epaules — aucun triplet valide ne peut etre forme.
    private static List<TickerCandle> BuildFlatTripletSeries(
        decimal shoulderHigh,
        decimal headHigh,
        decimal troughPrice,
        decimal finalClose)
    {
        var candles = new List<TickerCandle>();

        for (var i = 0; i < 30; i++)
        {
            candles.Add(Flat(troughPrice - i * 0.1m));
        }

        AddRamp(candles, from: troughPrice, to: shoulderHigh, steps: 8);
        candles.Add(Flat(shoulderHigh));
        AddRamp(candles, from: shoulderHigh, to: troughPrice, steps: 8);
        candles.Add(Flat(troughPrice));
        AddRamp(candles, from: troughPrice, to: headHigh, steps: 8);
        candles.Add(Flat(headHigh));
        AddRamp(candles, from: headHigh, to: troughPrice, steps: 8);
        candles.Add(Flat(troughPrice));
        AddRamp(candles, from: troughPrice, to: shoulderHigh, steps: 8);
        candles.Add(Flat(shoulderHigh));
        AddRamp(candles, from: shoulderHigh, to: troughPrice + 1m, steps: 8);

        candles.Add(Flat(finalClose));

        return candles;
    }

    // Serie avec un sommet en toute fin — les N dernieres bougies ne sont pas confirmables comme pivot.
    private static List<TickerCandle> BuildSeriesWithPeakAtEnd()
    {
        const int pivotHalfWindow = PatternThresholds.PivotHalfWindow;
        var candles = new List<TickerCandle>();

        for (var i = 0; i < 80; i++)
        {
            candles.Add(Flat(100m - i * 0.1m));
        }

        for (var i = 0; i < pivotHalfWindow; i++)
        {
            candles.Add(Flat(110m + i));
        }

        candles.Add(Candle(120m, 130m, 115m, 125m));

        return candles;
    }

    private static void AddRamp(List<TickerCandle> candles, decimal from, decimal to, int steps)
    {
        for (var i = 1; i <= steps; i++)
        {
            var price = from + (to - from) * i / steps;
            candles.Add(Flat(decimal.Round(price, 4)));
        }
    }

    // -----------------------------------------------------------------------
    // Fake provider — pas de framework de mock
    // -----------------------------------------------------------------------

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
