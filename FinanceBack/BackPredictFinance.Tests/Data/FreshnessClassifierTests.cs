using BackPredictFinance.Common.enums;
using BackPredictFinance.Services.ClientFinanceServices.Analysis;

namespace BackPredictFinance.Tests.Data;

public sealed class FreshnessClassifierTests
{
    private static readonly DateTime ReferenceMonday = new(2026, 6, 8, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Classify_WhenCheckedAtIsNull_ReturnsMissing()
    {
        var result = FreshnessClassifier.Classify(null, ReferenceMonday);
        Assert.Equal(FreshnessStatusEnum.Missing, result);
    }

    [Fact]
    public void Classify_WhenCheckedAtIsSameDay_ReturnsFresh()
    {
        var result = FreshnessClassifier.Classify(ReferenceMonday, ReferenceMonday);
        Assert.Equal(FreshnessStatusEnum.Fresh, result);
    }

    [Fact]
    public void Classify_WhenCheckedAtIsOneTradingDayBefore_ReturnsFresh()
    {
        var monday = new DateTime(2026, 6, 8, 12, 0, 0, DateTimeKind.Utc);
        var friday = new DateTime(2026, 6, 5, 12, 0, 0, DateTimeKind.Utc);
        var result = FreshnessClassifier.Classify(friday, monday);
        Assert.Equal(FreshnessStatusEnum.Fresh, result);
    }

    [Fact]
    public void Classify_WhenWeekendDoesNotInflateToStale_ReturnsCorrectBucket()
    {
        var monday = new DateTime(2026, 6, 8, 12, 0, 0, DateTimeKind.Utc);
        var wednesday = new DateTime(2026, 6, 3, 12, 0, 0, DateTimeKind.Utc);
        var result = FreshnessClassifier.Classify(wednesday, monday);
        Assert.Equal(FreshnessStatusEnum.Aging, result);
    }

    [Fact]
    public void Classify_WhenFourOrMoreTradingDays_ReturnsStale()
    {
        var wednesday = new DateTime(2026, 6, 10, 12, 0, 0, DateTimeKind.Utc);
        var previousWednesday = new DateTime(2026, 6, 3, 12, 0, 0, DateTimeKind.Utc);
        var result = FreshnessClassifier.Classify(previousWednesday, wednesday);
        Assert.Equal(FreshnessStatusEnum.Stale, result);
    }

    [Fact]
    public void Classify_WhenTwoTradingDays_ReturnsAging()
    {
        var friday = new DateTime(2026, 6, 5, 12, 0, 0, DateTimeKind.Utc);
        var wednesday = new DateTime(2026, 6, 3, 12, 0, 0, DateTimeKind.Utc);
        var result = FreshnessClassifier.Classify(wednesday, friday);
        Assert.Equal(FreshnessStatusEnum.Aging, result);
    }
}
