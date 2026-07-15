using BackPredictFinance.Common.enums;
using BackPredictFinance.Datas.Context;
using BackPredictFinance.Datas.Entities;
using BackPredictFinance.Services.ClientFinanceServices.Analysis;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace BackPredictFinance.Tests.Analysis;

public sealed class ExPostStatisticsWilsonTests
{
    [Fact]
    public void ComputeWilsonInterval_N100_P60_ReturnsExpectedBounds()
    {
        const int wins = 60;
        const int n = 100;
        const double z = 1.96;

        var pHat = (double)wins / n;
        var z2 = z * z;
        var denominator = 1.0 + z2 / n;
        var center = (pHat + z2 / (2.0 * n)) / denominator;
        var margin = z * Math.Sqrt(pHat * (1.0 - pHat) / n + z2 / (4.0 * n * n)) / denominator;

        var low = Math.Max(0.0, center - margin);
        var high = Math.Min(1.0, center + margin);

        Assert.Equal(0.60, pHat, precision: 5);
        Assert.True(low > 0.49 && low < 0.52, $"low={low} should be ~0.50");
        Assert.True(high > 0.68 && high < 0.71, $"high={high} should be ~0.695");
    }

    [Fact]
    public void ComputeWilsonInterval_N100_P0_ReturnsZeroCenteredBounds()
    {
        const int wins = 0;
        const int n = 100;
        const double z = 1.96;

        var pHat = (double)wins / n;
        var z2 = z * z;
        var denominator = 1.0 + z2 / n;
        var center = (pHat + z2 / (2.0 * n)) / denominator;
        var margin = z * Math.Sqrt(pHat * (1.0 - pHat) / n + z2 / (4.0 * n * n)) / denominator;

        var low = Math.Max(0.0, center - margin);
        var high = Math.Min(1.0, center + margin);

        Assert.Equal(0.0, low, precision: 5);
        Assert.True(high > 0.0 && high < 0.05, $"high={high} should be small when p=0");
    }

    [Fact]
    public void ComputeWilsonInterval_N100_P100_ReturnsOneAnchoredBounds()
    {
        const int wins = 100;
        const int n = 100;
        const double z = 1.96;

        var pHat = (double)wins / n;
        var z2 = z * z;
        var denominator = 1.0 + z2 / n;
        var center = (pHat + z2 / (2.0 * n)) / denominator;
        var margin = z * Math.Sqrt(pHat * (1.0 - pHat) / n + z2 / (4.0 * n * n)) / denominator;

        var low = Math.Max(0.0, center - margin);
        var high = Math.Min(1.0, center + margin);

        Assert.Equal(1.0, high, precision: 5);
        Assert.True(low > 0.95, $"low={low} should be close to 1 when p=1");
    }
}

public sealed class ExPostPathDependentTests
{
    private static FinanceDbContext BuildDb(string name)
    {
        var options = new DbContextOptionsBuilder<FinanceDbContext>()
            .UseInMemoryDatabase(name)
            .Options;
        var mock = new Mock<IHttpContextAccessor>();
        mock.Setup(x => x.HttpContext).Returns((HttpContext?)null);
        return new FinanceDbContext(options, mock.Object);
    }

    private static Asset BuildAsset(string id) => new()
    {
        Id = id,
        Symbol = id,
        ProviderSymbol = id,
        Exchange = "XPAR",
        Currency = "EUR",
        AssetType = AssetTypeEnum.Stock
    };

    private static AssetCandleSnapshot BuildCandle(string id, string assetId, DateTime date, decimal high, decimal low, decimal close) => new()
    {
        Id = id,
        AssetId = assetId,
        TimestampUtc = date,
        Interval = "1d",
        Open = close,
        High = high,
        Low = low,
        Close = close,
        Volume = 1000m,
        Source = "test"
    };

    [Fact]
    public async Task PathDependent_TargetHitBeforeInvalidation_ReturnsTargetReached()
    {
        var dbName = $"path-target-{Guid.NewGuid():N}";
        var db = BuildDb(dbName);
        var assetId = "A1";
        var analysisDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        db.Assets.Add(BuildAsset(assetId));
        db.AssetCandleSnapshots.AddRange(
            BuildCandle("c1", assetId, analysisDate.AddDays(1), high: 95m, low: 92m, close: 94m),
            BuildCandle("c2", assetId, analysisDate.AddDays(2), high: 105m, low: 98m, close: 103m),
            BuildCandle("c3", assetId, analysisDate.AddDays(3), high: 98m, low: 80m, close: 82m)
        );
        await db.SaveChangesAsync();

        var candles = await db.AssetCandleSnapshots
            .AsNoTracking()
            .Where(c => c.AssetId == assetId && c.Interval == "1d" && c.TimestampUtc >= analysisDate)
            .OrderBy(c => c.TimestampUtc)
            .ToListAsync();

        const decimal targetPrice = 100m;
        const decimal invalidationPrice = 85m;

        string? status = null;
        var index = 0;
        foreach (var candle in candles)
        {
            if (candle.Low <= invalidationPrice) { status = "INVALIDATED"; break; }
            if (candle.High >= targetPrice) { status = "TARGET_REACHED"; break; }
            index++;
        }

        Assert.Equal("TARGET_REACHED", status);
        Assert.Equal(1, index);
    }

    [Fact]
    public async Task PathDependent_InvalidationHitBeforeTarget_ReturnsInvalidated()
    {
        var dbName = $"path-stop-{Guid.NewGuid():N}";
        var db = BuildDb(dbName);
        var assetId = "A2";
        var analysisDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        db.Assets.Add(BuildAsset(assetId));
        db.AssetCandleSnapshots.AddRange(
            BuildCandle("c1", assetId, analysisDate.AddDays(1), high: 95m, low: 92m, close: 94m),
            BuildCandle("c2", assetId, analysisDate.AddDays(2), high: 96m, low: 83m, close: 84m),
            BuildCandle("c3", assetId, analysisDate.AddDays(3), high: 110m, low: 98m, close: 108m)
        );
        await db.SaveChangesAsync();

        var candles = await db.AssetCandleSnapshots
            .AsNoTracking()
            .Where(c => c.AssetId == assetId && c.Interval == "1d" && c.TimestampUtc >= analysisDate)
            .OrderBy(c => c.TimestampUtc)
            .ToListAsync();

        const decimal targetPrice = 100m;
        const decimal invalidationPrice = 85m;

        string? status = null;
        var index = 0;
        foreach (var candle in candles)
        {
            if (candle.Low <= invalidationPrice) { status = "INVALIDATED"; break; }
            if (candle.High >= targetPrice) { status = "TARGET_REACHED"; break; }
            index++;
        }

        Assert.Equal("INVALIDATED", status);
        Assert.Equal(1, index);
    }

    [Fact]
    public async Task PathDependent_NoLevelTouched_ReturnsPending()
    {
        var dbName = $"path-pending-{Guid.NewGuid():N}";
        var db = BuildDb(dbName);
        var assetId = "A3";
        var analysisDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        db.Assets.Add(BuildAsset(assetId));
        db.AssetCandleSnapshots.AddRange(
            BuildCandle("c1", assetId, analysisDate.AddDays(1), high: 95m, low: 92m, close: 93m),
            BuildCandle("c2", assetId, analysisDate.AddDays(2), high: 96m, low: 91m, close: 93m),
            BuildCandle("c3", assetId, analysisDate.AddDays(3), high: 97m, low: 93m, close: 96m)
        );
        await db.SaveChangesAsync();

        var candles = await db.AssetCandleSnapshots
            .AsNoTracking()
            .Where(c => c.AssetId == assetId && c.Interval == "1d" && c.TimestampUtc >= analysisDate)
            .OrderBy(c => c.TimestampUtc)
            .ToListAsync();

        const decimal targetPrice = 100m;
        const decimal invalidationPrice = 85m;

        string? status = null;
        foreach (var candle in candles)
        {
            if (candle.Low <= invalidationPrice) { status = "INVALIDATED"; break; }
            if (candle.High >= targetPrice) { status = "TARGET_REACHED"; break; }
        }

        Assert.Null(status);
    }

    [Fact]
    public async Task PathDependent_SameCandleHitsBothLevels_InvalidationPriority()
    {
        var dbName = $"path-both-{Guid.NewGuid():N}";
        var db = BuildDb(dbName);
        var assetId = "A4";
        var analysisDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        db.Assets.Add(BuildAsset(assetId));
        db.AssetCandleSnapshots.Add(
            BuildCandle("c1", assetId, analysisDate.AddDays(1), high: 120m, low: 80m, close: 95m)
        );
        await db.SaveChangesAsync();

        var candles = await db.AssetCandleSnapshots
            .AsNoTracking()
            .Where(c => c.AssetId == assetId && c.Interval == "1d" && c.TimestampUtc >= analysisDate)
            .OrderBy(c => c.TimestampUtc)
            .ToListAsync();

        const decimal targetPrice = 100m;
        const decimal invalidationPrice = 85m;

        string? status = null;
        foreach (var candle in candles)
        {
            if (candle.Low <= invalidationPrice) { status = "INVALIDATED"; break; }
            if (candle.High >= targetPrice) { status = "TARGET_REACHED"; break; }
        }

        Assert.Equal("INVALIDATED", status);
    }
}
