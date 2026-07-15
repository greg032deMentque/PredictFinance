using BackPredictFinance.Common.AnalysisV1;
using BackPredictFinance.Common.MarketData;
using BackPredictFinance.Patterns.Abstractions;
using BackPredictFinance.Patterns.Common;
using BackPredictFinance.Patterns.Definitions;

namespace BackPredictFinance.Tests.Analysis;

public sealed class InverseHeadAndShouldersPatternTests
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
    public async Task Analyze_ValidTriplet_WithBreakoutAboveNeckline_ReturnsConfirmedPhase()
    {
        var candles = BuildInverseHsSeries(
            contextPrice: 100m,
            leftShoulderLow: 85m,
            headLow: 70m,
            rightShoulderLow: 86m,
            necklineApprox: 100m,
            finalClose: 103m);

        var definition = new InverseHeadAndShouldersReversalPatternDefinition(new FakePatternMarketDataProvider(candles));
        var artifact = await definition.ExecuteAsync(BuildRequest());

        var pattern = artifact.Patterns[0];
        Assert.Equal("inverse_hs_breakout_confirmed", pattern.Phase);
        Assert.True(pattern.ContractAssessment.Detection.IsCompatible);
        Assert.Equal("VALIDATED", pattern.ContractAssessment.Validation.State);
        Assert.True(pattern.Confidence >= 0.80m);
    }

    [Fact]
    public async Task Analyze_ValidTriplet_NoPriceBreakout_ReturnsFormingPhase()
    {
        var candles = BuildInverseHsSeries(
            contextPrice: 100m,
            leftShoulderLow: 85m,
            headLow: 70m,
            rightShoulderLow: 86m,
            necklineApprox: 100m,
            finalClose: 95m);

        var definition = new InverseHeadAndShouldersReversalPatternDefinition(new FakePatternMarketDataProvider(candles));
        var artifact = await definition.ExecuteAsync(BuildRequest());

        var pattern = artifact.Patterns[0];
        Assert.Equal("inverse_hs_forming", pattern.Phase);
        Assert.True(pattern.ContractAssessment.Detection.IsCompatible);
        Assert.Equal("NOT_VALIDATED", pattern.ContractAssessment.Validation.State);
    }

    [Fact]
    public async Task Analyze_HeadNotDeepEnough_ReturnsNotConfirmed()
    {
        var candles = BuildFlatTripleSeries(
            contextPrice: 100m,
            shoulderLow: 85m,
            headLow: 85m,
            finalClose: 95m);

        var definition = new InverseHeadAndShouldersReversalPatternDefinition(new FakePatternMarketDataProvider(candles));
        var artifact = await definition.ExecuteAsync(BuildRequest());

        var pattern = artifact.Patterns[0];
        Assert.Equal("inverse_hs_not_confirmed", pattern.Phase);
        Assert.False(pattern.ContractAssessment.Detection.IsCompatible);
    }

    [Fact]
    public async Task Analyze_NoLookAhead_LastNBarsNotUsedAsPivots()
    {
        var candles = BuildSeriesWithHeadAtEnd();

        var definition = new InverseHeadAndShouldersReversalPatternDefinition(new FakePatternMarketDataProvider(candles));
        var artifact = await definition.ExecuteAsync(BuildRequest());

        var pattern = artifact.Patterns[0];
        Assert.NotEqual("inverse_hs_breakout_confirmed", pattern.Phase);
        Assert.NotEqual("inverse_hs_forming", pattern.Phase);
    }

    // -----------------------------------------------------------------------
    // Helpers de construction de series synthetiques
    // -----------------------------------------------------------------------

    // Construit une serie IH&S canonique : contexte plat, epaule gauche, remontee,
    // tete plus basse, remontee, epaule droite, remontee, puis finalClose.
    // Les transitions sont strictement monotones (rampes) pour generer des pivots nets.
    private static List<TickerCandle> BuildInverseHsSeries(
        decimal contextPrice,
        decimal leftShoulderLow,
        decimal headLow,
        decimal rightShoulderLow,
        decimal necklineApprox,
        decimal finalClose)
    {
        var candles = new List<TickerCandle>();

        for (var i = 0; i < 30; i++)
        {
            candles.Add(Flat(contextPrice + (30 - i)));
        }

        AddRamp(candles, from: contextPrice, to: leftShoulderLow, steps: 8);
        candles.Add(Flat(leftShoulderLow));
        AddRamp(candles, from: leftShoulderLow, to: necklineApprox, steps: 8);

        AddRamp(candles, from: necklineApprox, to: headLow, steps: 8);
        candles.Add(Flat(headLow));
        AddRamp(candles, from: headLow, to: necklineApprox, steps: 8);

        AddRamp(candles, from: necklineApprox, to: rightShoulderLow, steps: 8);
        candles.Add(Flat(rightShoulderLow));
        AddRamp(candles, from: rightShoulderLow, to: necklineApprox, steps: 8);

        candles.Add(Flat(finalClose));

        return candles;
    }

    // Serie avec trois creux a la meme hauteur — la tete n'est pas plus profonde.
    private static List<TickerCandle> BuildFlatTripleSeries(
        decimal contextPrice,
        decimal shoulderLow,
        decimal headLow,
        decimal finalClose)
    {
        var candles = new List<TickerCandle>();

        for (var i = 0; i < 30; i++)
        {
            candles.Add(Flat(contextPrice + (30 - i)));
        }

        AddRamp(candles, from: contextPrice, to: shoulderLow, steps: 8);
        candles.Add(Flat(shoulderLow));
        AddRamp(candles, from: shoulderLow, to: contextPrice, steps: 8);

        AddRamp(candles, from: contextPrice, to: headLow, steps: 8);
        candles.Add(Flat(headLow));
        AddRamp(candles, from: headLow, to: contextPrice, steps: 8);

        AddRamp(candles, from: contextPrice, to: shoulderLow, steps: 8);
        candles.Add(Flat(shoulderLow));
        AddRamp(candles, from: shoulderLow, to: contextPrice, steps: 8);

        candles.Add(Flat(finalClose));

        return candles;
    }

    // Serie avec un creux profond place dans les dernieres N bougies —
    // le look-ahead guard empeche de le qualifier comme pivot confirme.
    private static List<TickerCandle> BuildSeriesWithHeadAtEnd()
    {
        const int pivotHalfWindow = PatternThresholds.PivotHalfWindow;
        var candles = new List<TickerCandle>();

        for (var i = 0; i < 80; i++)
        {
            candles.Add(Flat(100m + (80 - i)));
        }

        for (var i = 0; i < pivotHalfWindow; i++)
        {
            candles.Add(Flat(70m - i));
        }

        candles.Add(Candle(60m, 70m, 55m, 58m));

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
