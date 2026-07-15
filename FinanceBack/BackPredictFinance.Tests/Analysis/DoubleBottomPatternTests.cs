using BackPredictFinance.Common.AnalysisV1;
using BackPredictFinance.Common.MarketData;
using BackPredictFinance.Patterns.Abstractions;
using BackPredictFinance.Patterns.Common;
using BackPredictFinance.Patterns.Definitions;

namespace BackPredictFinance.Tests.Analysis;

public sealed class DoubleBottomPatternTests
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
    public async Task Analyze_TwoEqualLows_WithBreakoutAboveNeckline_ReturnsConfirmedPhase()
    {
        var candles = BuildDoubleBottomSeries(
            basePrice: 100m,
            troughPrice: 85m,
            necklinePrice: 100m,
            finalClose: 103m);

        var definition = new DoubleBottomReversalPatternDefinition(new FakePatternMarketDataProvider(candles));
        var artifact = await definition.ExecuteAsync(BuildRequest());

        var pattern = artifact.Patterns[0];
        Assert.Equal("double_bottom_breakout_confirmed", pattern.Phase);
        Assert.True(pattern.ContractAssessment.Detection.IsCompatible);
        Assert.True(pattern.ContractAssessment.Validation.State == "VALIDATED");
        Assert.True(pattern.Confidence >= 0.80m);
    }

    [Fact]
    public async Task Analyze_TwoLowsTooFarApart_ReturnNotConfirmed()
    {
        var candles = BuildSeriesWithOneIsolatedTrough(
            troughPrice: 85m,
            contextPrice: 100m,
            finalClose: 95m);

        var definition = new DoubleBottomReversalPatternDefinition(new FakePatternMarketDataProvider(candles));
        var artifact = await definition.ExecuteAsync(BuildRequest());

        var pattern = artifact.Patterns[0];
        Assert.Equal("double_bottom_not_confirmed", pattern.Phase);
        Assert.False(pattern.ContractAssessment.Detection.IsCompatible);
    }

    [Fact]
    public async Task Analyze_NoLookAhead_LastNBarsNotUsedAsPivots()
    {
        var candles = BuildSeriesWithTroughAtEnd();

        var definition = new DoubleBottomReversalPatternDefinition(new FakePatternMarketDataProvider(candles));
        var artifact = await definition.ExecuteAsync(BuildRequest());

        var pattern = artifact.Patterns[0];
        Assert.NotEqual("double_bottom_breakout_confirmed", pattern.Phase);
        Assert.NotEqual("double_bottom_forming", pattern.Phase);
    }

    [Fact]
    public async Task Analyze_BreakoutConfirmed_TargetPriceSetToMeasuredMove()
    {
        const decimal troughPrice = 85m;
        const decimal necklinePrice = 100m;
        const decimal finalClose = 103m;
        const decimal expectedFigureHeight = necklinePrice - troughPrice;
        const decimal expectedTarget = necklinePrice + expectedFigureHeight;

        var candles = BuildDoubleBottomSeries(
            basePrice: 100m,
            troughPrice: troughPrice,
            necklinePrice: necklinePrice,
            finalClose: finalClose);

        var definition = new DoubleBottomReversalPatternDefinition(new FakePatternMarketDataProvider(candles));
        var artifact = await definition.ExecuteAsync(BuildRequest());

        var pattern = artifact.Patterns[0];
        Assert.Equal("double_bottom_breakout_confirmed", pattern.Phase);
        Assert.NotNull(pattern.TargetPrice);
        Assert.True(
            pattern.TargetPrice >= expectedTarget - 0.5m && pattern.TargetPrice <= expectedTarget + 0.5m,
            $"TargetPrice attendu ~ {expectedTarget}, obtenu {pattern.TargetPrice}");
    }

    // -----------------------------------------------------------------------
    // Helpers de construction de series synthetiques
    // -----------------------------------------------------------------------

    // Construit une serie W avec deux creux a troughPrice / troughPrice+0.5, neckline a necklinePrice.
    // Toutes les transitions sont strictement monotones (pas de plateau) pour eviter les faux pivots.
    // Structure : descente lineaire vers creux1, remontee vers neckline, descente vers creux2,
    // remontee vers neckline, puis finalClose.
    private static List<TickerCandle> BuildDoubleBottomSeries(
        decimal basePrice,
        decimal troughPrice,
        decimal necklinePrice,
        decimal finalClose)
    {
        var candles = new List<TickerCandle>();

        // Padding initial — bougies decroissantes pour ne pas creer de plateau
        for (var i = 0; i < 30; i++)
        {
            candles.Add(Flat(basePrice + 30m - i));
        }

        AddRamp(candles, from: basePrice, to: troughPrice, steps: 10);
        candles.Add(Flat(troughPrice));
        AddRamp(candles, from: troughPrice, to: necklinePrice, steps: 10);
        AddRamp(candles, from: necklinePrice, to: troughPrice + 0.5m, steps: 10);
        candles.Add(Flat(troughPrice + 0.5m));
        AddRamp(candles, from: troughPrice + 0.5m, to: necklinePrice, steps: 10);

        candles.Add(Flat(finalClose));

        return candles;
    }

    // Serie a un seul creux isole — impossible de former un double bottom.
    private static List<TickerCandle> BuildSeriesWithOneIsolatedTrough(
        decimal troughPrice,
        decimal contextPrice,
        decimal finalClose)
    {
        var candles = new List<TickerCandle>();

        for (var i = 0; i < 40; i++)
        {
            candles.Add(Flat(contextPrice + 40m - i));
        }

        AddRamp(candles, from: contextPrice, to: troughPrice, steps: 10);
        candles.Add(Flat(troughPrice));
        AddRamp(candles, from: troughPrice, to: finalClose, steps: 10);

        candles.Add(Flat(finalClose));

        return candles;
    }

    private static List<TickerCandle> BuildSeriesWithTroughAtEnd()
    {
        const int pivotHalfWindow = PatternThresholds.PivotHalfWindow;
        var candles = new List<TickerCandle>();

        for (var i = 0; i < 60; i++)
        {
            candles.Add(Flat(100m + (60 - i)));
        }

        for (var i = 0; i < pivotHalfWindow; i++)
        {
            candles.Add(Flat(80m - i));
        }

        candles.Add(Candle(70m, 80m, 70m, 72m));

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
