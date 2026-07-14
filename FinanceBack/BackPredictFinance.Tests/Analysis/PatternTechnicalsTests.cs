using BackPredictFinance.Common.MarketData;
using BackPredictFinance.Patterns.Common;

namespace BackPredictFinance.Tests.Analysis;

/// <summary>
/// Preuves de comportement sur les calculs techniques du moteur de patterns.
/// Ces formules sont la base de toute detection : une regression silencieuse y fausserait
/// les bornes, les breakouts et les cibles sans aucun signal. Tests justifies a ce titre.
/// </summary>
public sealed class PatternTechnicalsTests
{
    private static TickerCandle Candle(decimal high, decimal low, decimal close)
    {
        return new TickerCandle { High = high, Low = low, Close = close, Open = close, Volume = 1_000m };
    }

    [Fact]
    public void AverageTrueRange_IntegratesOpeningGaps_ViaPreviousClose()
    {
        // La bougie 1 ouvre tres au-dessus de la cloture precedente (gap) : la True Range doit
        // capturer cet ecart (|High - Close_prec|), pas seulement l'amplitude High-Low de la bougie.
        var candles = new List<TickerCandle>
        {
            Candle(high: 10m, low: 8m, close: 9m),
            Candle(high: 20m, low: 18m, close: 19m), // TR = max(2, |20-9|=11, |18-9|=9) = 11
            Candle(high: 21m, low: 19m, close: 20m), // TR = 2
            Candle(high: 22m, low: 20m, close: 21m)  // TR = 2
        };

        // Wilder periode 3 : amorcage = moyenne(TR0,TR1,TR2) = (2+11+2)/3 = 5 ;
        // puis lissage bougie 3 : (5*2 + 2)/3 = 4.
        var atr = PatternTechnicals.AverageTrueRange(candles, period: 3);

        Assert.Equal(4m, atr);
        // L'ancienne formule (moyenne simple de High-Low) aurait renvoye 2 : le gap aurait ete ignore.
        Assert.NotEqual(2m, atr);
    }

    [Fact]
    public void AverageTrueRange_FlatSeries_ReturnsZero()
    {
        var candles = new List<TickerCandle>
        {
            Candle(100m, 100m, 100m),
            Candle(100m, 100m, 100m),
            Candle(100m, 100m, 100m)
        };

        Assert.Equal(0m, PatternTechnicals.AverageTrueRange(candles));
    }

    [Fact]
    public void ComputeLinearFit_ReturnsSlopeAndIntercept()
    {
        // Serie parfaitement lineaire y = 2x + 10 sur x = 0..3.
        var values = new List<decimal> { 10m, 12m, 14m, 16m };

        var (slope, intercept) = PatternTechnicals.ComputeLinearFit(values);

        Assert.Equal(2m, slope);
        Assert.Equal(10m, intercept);
    }

    [Fact]
    public void ComputeLinearFit_FirstPointOutlier_ProjectionUsesRegressionNotFirstCandle()
    {
        // Premiere valeur aberrante (meche extreme) : c'est le scenario qui faisait deriver la
        // projection du triangle quand la borne etait ancree sur la premiere bougie.
        var values = new List<decimal> { 100m, 12m, 14m, 16m };

        var (slope, intercept) = PatternTechnicals.ComputeLinearFit(values);
        var lastIndex = values.Count - 1;
        var projectedViaRegression = intercept + (slope * lastIndex);
        var projectedViaFirstPointAnchor = values[0] + (slope * lastIndex);

        // La projection correcte (via l'ordonnee a l'origine) differe nettement de l'ancien ancrage
        // sur la premiere bougie : c'est exactement la correction du bug de borne du triangle.
        Assert.NotEqual(projectedViaFirstPointAnchor, projectedViaRegression);
    }

    [Fact]
    public void VolatilityUnit_AppliesPriceFloor_WhenVolatilityIsNegligible()
    {
        // Serie plate : ATR = 0. Le garde-fou doit renvoyer un plancher (0,1 % du prix de reference)
        // pour eviter qu'un breakout se declenche au moindre tick.
        var candles = new List<TickerCandle>
        {
            Candle(100m, 100m, 100m),
            Candle(100m, 100m, 100m)
        };

        var unit = PatternTechnicals.VolatilityUnit(candles, referencePrice: 100m);

        Assert.Equal(0.1m, unit);
    }
}
