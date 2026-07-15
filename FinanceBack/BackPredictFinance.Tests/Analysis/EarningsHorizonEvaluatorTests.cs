using BackPredictFinance.Common.AnalysisV1;

namespace BackPredictFinance.Tests.Analysis;

public sealed class EarningsHorizonEvaluatorTests
{
    private static readonly DateTime EmissionDateUtc = new(2026, 1, 1, 8, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void IsWithinHorizon_EarningsExactlyOnLastDayOfWindow_ReturnsTrue()
    {
        var earningsDateUtc = EmissionDateUtc.AddDays(20);

        var result = EarningsHorizonEvaluator.IsWithinHorizon(earningsDateUtc, 20, EmissionDateUtc);

        Assert.True(result);
    }

    [Fact]
    public void IsWithinHorizon_EarningsOneDayAfterWindow_ReturnsFalse()
    {
        var earningsDateUtc = EmissionDateUtc.AddDays(21);

        var result = EarningsHorizonEvaluator.IsWithinHorizon(earningsDateUtc, 20, EmissionDateUtc);

        Assert.False(result);
    }

    [Fact]
    public void IsWithinHorizon_HorizonDaysIsZero_ReturnsFalseEvenIfEarningsIsImminent()
    {
        var earningsDateUtc = EmissionDateUtc.AddHours(1);

        var result = EarningsHorizonEvaluator.IsWithinHorizon(earningsDateUtc, 0, EmissionDateUtc);

        Assert.False(result);
    }

    [Fact]
    public void IsWithinHorizon_EarningsDateUtcIsNull_ReturnsFalse()
    {
        var result = EarningsHorizonEvaluator.IsWithinHorizon(null, 20, EmissionDateUtc);

        Assert.False(result);
    }

    [Fact]
    public void IsWithinHorizon_EarningsDateBeforeEmission_ReturnsFalse()
    {
        var earningsDateUtc = EmissionDateUtc.AddDays(-5);

        var result = EarningsHorizonEvaluator.IsWithinHorizon(earningsDateUtc, 20, EmissionDateUtc);

        Assert.False(result);
    }

    [Fact]
    public void IsWithinHorizon_EarningsWithinWindow_ReturnsTrue()
    {
        var earningsDateUtc = EmissionDateUtc.AddDays(10);

        var result = EarningsHorizonEvaluator.IsWithinHorizon(earningsDateUtc, 20, EmissionDateUtc);

        Assert.True(result);
    }
}
