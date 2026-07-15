using BackPredictFinance.Common.enums;
using BackPredictFinance.Datas.Entities;
using BackPredictFinance.Services.ClientFinanceServices.Analysis;

namespace BackPredictFinance.Tests.Analysis;

public sealed class SignalDirectionalScanEvaluatorTests
{
    private static AssetCandleSnapshot BuildCandle(DateTime timestampUtc, decimal high, decimal low, decimal close) => new()
    {
        Id = Guid.NewGuid().ToString("N"),
        AssetId = "asset-scan-1",
        Interval = "1d",
        TimestampUtc = timestampUtc,
        Open = close,
        High = high,
        Low = low,
        Close = close,
        Volume = 1000m,
        Source = "test"
    };

    [Fact]
    public void ScanForFirstHit_BearishDirection_TargetBelowPriceReached_ReturnsTargetHit()
    {
        var day = new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc);
        var candles = new List<AssetCandleSnapshot>
        {
            BuildCandle(day, high: 105m, low: 98m, close: 100m),
            BuildCandle(day.AddDays(1), high: 100m, low: 88m, close: 90m)
        };

        var hit = SignalDirectionalScanEvaluator.ScanForFirstHit(
            candles,
            PatternDirectionEnum.Bearish,
            targetPrice: 90m,
            invalidationPrice: 110m);

        Assert.Equal(SignalDirectionalHitKind.TargetHit, hit.Kind);
        Assert.Equal(candles[1].TimestampUtc, hit.Candle!.TimestampUtc);
    }

    [Fact]
    public void ScanForFirstHit_BearishDirection_InvalidationAbovePriceReached_ReturnsInvalidationHit()
    {
        var day = new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc);
        var candles = new List<AssetCandleSnapshot>
        {
            BuildCandle(day, high: 105m, low: 98m, close: 100m),
            BuildCandle(day.AddDays(1), high: 112m, low: 105m, close: 111m)
        };

        var hit = SignalDirectionalScanEvaluator.ScanForFirstHit(
            candles,
            PatternDirectionEnum.Bearish,
            targetPrice: 90m,
            invalidationPrice: 110m);

        Assert.Equal(SignalDirectionalHitKind.InvalidationHit, hit.Kind);
        Assert.Equal(candles[1].TimestampUtc, hit.Candle!.TimestampUtc);
    }

    [Fact]
    public void ScanForFirstHit_BearishDirection_PriceDropsButNotToTarget_DoesNotReturnInvalidationHit()
    {
        var day = new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc);
        var candles = new List<AssetCandleSnapshot>
        {
            BuildCandle(day, high: 104m, low: 96m, close: 99m)
        };

        var hit = SignalDirectionalScanEvaluator.ScanForFirstHit(
            candles,
            PatternDirectionEnum.Bearish,
            targetPrice: 90m,
            invalidationPrice: 110m);

        Assert.Equal(SignalDirectionalHitKind.None, hit.Kind);
    }

    [Fact]
    public void ScanForFirstHit_BullishDirection_TargetAbovePriceReached_ReturnsTargetHit()
    {
        var day = new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc);
        var candles = new List<AssetCandleSnapshot>
        {
            BuildCandle(day, high: 105m, low: 98m, close: 100m),
            BuildCandle(day.AddDays(1), high: 122m, low: 110m, close: 121m)
        };

        var hit = SignalDirectionalScanEvaluator.ScanForFirstHit(
            candles,
            PatternDirectionEnum.Bullish,
            targetPrice: 120m,
            invalidationPrice: 90m);

        Assert.Equal(SignalDirectionalHitKind.TargetHit, hit.Kind);
        Assert.Equal(candles[1].TimestampUtc, hit.Candle!.TimestampUtc);
    }

    [Fact]
    public void ScanForFirstHit_BullishDirection_InvalidationBelowPriceReached_ReturnsInvalidationHit()
    {
        var day = new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc);
        var candles = new List<AssetCandleSnapshot>
        {
            BuildCandle(day, high: 105m, low: 98m, close: 100m),
            BuildCandle(day.AddDays(1), high: 95m, low: 85m, close: 87m)
        };

        var hit = SignalDirectionalScanEvaluator.ScanForFirstHit(
            candles,
            PatternDirectionEnum.Bullish,
            targetPrice: 120m,
            invalidationPrice: 90m);

        Assert.Equal(SignalDirectionalHitKind.InvalidationHit, hit.Kind);
        Assert.Equal(candles[1].TimestampUtc, hit.Candle!.TimestampUtc);
    }

    [Fact]
    public void ScanForFirstHit_UnknownDirection_MissingPrices_ReturnsNoneSafely()
    {
        var day = new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc);
        var candles = new List<AssetCandleSnapshot>
        {
            BuildCandle(day, high: 105m, low: 98m, close: 100m)
        };

        var hit = SignalDirectionalScanEvaluator.ScanForFirstHit(
            candles,
            PatternDirectionEnum.Unknown,
            targetPrice: null,
            invalidationPrice: null);

        Assert.Equal(SignalDirectionalHitKind.None, hit.Kind);
        Assert.Null(hit.Candle);
    }

    [Fact]
    public void ScanForFirstHit_UnknownDirection_WithPrices_FallsBackToBullishComparison()
    {
        var day = new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc);
        var candles = new List<AssetCandleSnapshot>
        {
            BuildCandle(day, high: 122m, low: 110m, close: 121m)
        };

        var hit = SignalDirectionalScanEvaluator.ScanForFirstHit(
            candles,
            PatternDirectionEnum.Unknown,
            targetPrice: 120m,
            invalidationPrice: 90m);

        Assert.Equal(SignalDirectionalHitKind.TargetHit, hit.Kind);
    }
}
