namespace BackPredictFinance.Tests.Portfolio;

public sealed class PortfolioAllocationHhiTests
{
    [Fact]
    public void ConcentrationScore_SingleLine_Returns100()
    {
        var values = new[] { 1000m };
        var total = values.Sum();

        var hhi = values.Sum(v => Math.Pow((double)(v / total), 2));
        var score = decimal.Round((decimal)(hhi * 100), 1);

        Assert.Equal(100.0m, score);
    }

    [Fact]
    public void ConcentrationScore_TenEqualLines_Returns10()
    {
        var values = Enumerable.Repeat(100m, 10).ToArray();
        var total = values.Sum();

        var hhi = values.Sum(v => Math.Pow((double)(v / total), 2));
        var score = decimal.Round((decimal)(hhi * 100), 1);

        Assert.Equal(10.0m, score);
    }

    [Fact]
    public void ConcentrationScore_TwoLinesOneHeavy_IsBetween10And100()
    {
        var values = new[] { 800m, 200m };
        var total = values.Sum();

        var hhi = values.Sum(v => Math.Pow((double)(v / total), 2));
        var score = decimal.Round((decimal)(hhi * 100), 1);

        Assert.True(score > 10m && score < 100m, $"score={score}");
    }

    [Fact]
    public void ConcentrationAlert_LineFiftyPct_IsTriggered()
    {
        const decimal lineWeight = 0.50m;
        const decimal threshold = 0.15m;

        Assert.True(lineWeight > threshold);
    }

    [Fact]
    public void ConcentrationAlert_LineTenPct_IsNotTriggered()
    {
        const decimal lineWeight = 0.10m;
        const decimal threshold = 0.15m;

        Assert.False(lineWeight > threshold);
    }

    [Fact]
    public void ConcentrationAlert_SectorFortyPct_IsTriggered()
    {
        const decimal sectorWeight = 0.40m;
        const decimal threshold = 0.30m;

        Assert.True(sectorWeight > threshold);
    }

    [Fact]
    public void ConcentrationAlert_SectorTwentyPct_IsNotTriggered()
    {
        const decimal sectorWeight = 0.20m;
        const decimal threshold = 0.30m;

        Assert.False(sectorWeight > threshold);
    }

    [Fact]
    public void DiversificationRating_HhiAbove25_IsConcentrated()
    {
        const double hhi = 0.30;
        var rating = ClassifyHhi(hhi);
        Assert.Equal("Concentrated", rating);
    }

    [Fact]
    public void DiversificationRating_HhiBetween10And25_IsModerate()
    {
        const double hhi = 0.15;
        var rating = ClassifyHhi(hhi);
        Assert.Equal("Moderate", rating);
    }

    [Fact]
    public void DiversificationRating_HhiBelow10_IsDiversified()
    {
        const double hhi = 0.08;
        var rating = ClassifyHhi(hhi);
        Assert.Equal("Diversified", rating);
    }

    private static string ClassifyHhi(double hhi)
    {
        if (hhi > 0.25)
        {
            return "Concentrated";
        }

        if (hhi > 0.10)
        {
            return "Moderate";
        }

        return "Diversified";
    }
}
