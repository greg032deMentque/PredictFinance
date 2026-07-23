using BackPredictFinance.Common.AnalysisV1;
using BackPredictFinance.Common.MarketData;
using BackPredictFinance.Patterns.Abstractions;
using BackPredictFinance.Patterns.Definitions;

namespace BackPredictFinance.Tests.Analysis;

public sealed class SymmetricalTriangleContinuationPatternTests
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

    private static List<TickerCandle> BuildPadding(int count = 4, decimal price = 275m)
    {
        return Enumerable.Range(0, count).Select(_ => Flat(price)).ToList();
    }

    private static List<TickerCandle> BuildPriorUptrend()
    {
        var candles = new List<TickerCandle>();
        for (var i = 0; i < 20; i++)
        {
            candles.Add(Flat(70m + (89m - 70m) * i / 19m));
        }

        return candles;
    }

    private static List<TickerCandle> BuildFlatPrior()
    {
        return Enumerable.Range(0, 20).Select(_ => Flat(100m)).ToList();
    }

    // 23 bougies convergentes : plus-hauts decroissants (505 -> 285, pas -10), plus-bas croissants
    // (45 -> 265, pas +10), cloture constante a 275 (le milieu). Le "gap de depart" (D=230) est fixe
    // au minimum viable (D = 23 * pas) pour que le pattern converge tout juste vers son apex a la
    // bougie 24 sans jamais croiser High/Low, ce qui minimise le True Range moyen (donc l'ATR) tout
    // en gardant une marge de 23*pas = 230 disponible pour une bougie de breakout ulterieure.
    private static List<TickerCandle> BuildConvergingFirst23()
    {
        var candles = new List<TickerCandle>();
        for (var i = 0; i < 23; i++)
        {
            var high = 505m - 10m * i;
            var low = 45m + 10m * i;
            candles.Add(Candle(open: 275m, high: high, low: low, close: 275m));
        }

        return candles;
    }

    [Fact]
    public async Task Analyze_FlatNonConvergingRange_ReturnsCompressionNotConfirmed()
    {
        var candles = BuildPadding()
            .Concat(BuildFlatPrior())
            .Concat(Enumerable.Range(0, 24).Select(_ => Candle(open: 275m, high: 290m, low: 260m, close: 275m)))
            .ToList();

        var definition = new SymmetricalTriangleContinuationAnalysisPatternDefinition(new FakePatternMarketDataProvider(candles));
        var artifact = await definition.ExecuteAsync(BuildRequest());

        var pattern = artifact.Patterns[0];
        Assert.Equal("compression_not_confirmed", pattern.Phase);
        Assert.False(pattern.ContractAssessment.Detection.IsCompatible);
    }

    [Fact]
    public async Task Analyze_ConvergingTriangleWithPriorUptrend_NoBreakout_ReturnsBullishCompressing()
    {
        // La 24e bougie prolonge la convergence exactement jusqu'a l'apex (275/275/275/275) :
        // aucune cassure, la compression reste bilaterale jusqu'a preuve du contraire.
        var candles = BuildPadding()
            .Concat(BuildPriorUptrend())
            .Concat(BuildConvergingFirst23())
            .Append(Candle(open: 275m, high: 275m, low: 275m, close: 275m))
            .ToList();

        var definition = new SymmetricalTriangleContinuationAnalysisPatternDefinition(new FakePatternMarketDataProvider(candles));
        var artifact = await definition.ExecuteAsync(BuildRequest());

        var pattern = artifact.Patterns[0];
        Assert.Equal("bullish_triangle_compressing", pattern.Phase);
        Assert.True(pattern.ContractAssessment.Detection.IsCompatible);
        Assert.False(pattern.ContractAssessment.Validation.State == "VALIDATED");
    }

    [Fact]
    public async Task Analyze_ConvergingTriangleWithPriorUptrend_UpsideBreakout_ReturnsBullishBreakoutConfirmed()
    {
        // La 24e bougie casse tres au-dessus de la borne haute projetee, tout en restant sous le
        // plus-haut de la premiere moitie (505) pour ne pas casser la condition de compression brute
        // (secondHalfHigh < firstHalfHigh).
        var candles = BuildPadding()
            .Concat(BuildPriorUptrend())
            .Concat(BuildConvergingFirst23())
            .Append(Candle(open: 490m, high: 495m, low: 488m, close: 493m))
            .ToList();

        var definition = new SymmetricalTriangleContinuationAnalysisPatternDefinition(new FakePatternMarketDataProvider(candles));
        var artifact = await definition.ExecuteAsync(BuildRequest());

        var pattern = artifact.Patterns[0];
        Assert.Equal("bullish_breakout_confirmed", pattern.Phase);
        Assert.True(pattern.ContractAssessment.Detection.IsCompatible);
        Assert.Equal("VALIDATED", pattern.ContractAssessment.Validation.State);
    }

    [Fact]
    public async Task Analyze_ConvergingTriangleWithPriorUptrend_DownsideBreakout_ReturnsOppositeInvalidated()
    {
        // Cassure symetrique vers le bas, alors que la tendance prealable est haussiere : le
        // scenario de continuation est invalide par une sortie opposee.
        var candles = BuildPadding()
            .Concat(BuildPriorUptrend())
            .Concat(BuildConvergingFirst23())
            .Append(Candle(open: 60m, high: 65m, low: 55m, close: 57m))
            .ToList();

        var definition = new SymmetricalTriangleContinuationAnalysisPatternDefinition(new FakePatternMarketDataProvider(candles));
        var artifact = await definition.ExecuteAsync(BuildRequest());

        var pattern = artifact.Patterns[0];
        Assert.Equal("opposite_breakout_invalidated", pattern.Phase);
        Assert.False(pattern.ContractAssessment.Detection.IsCompatible);
        Assert.True(pattern.ContractAssessment.Invalidation.State == "INVALIDATED");
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
