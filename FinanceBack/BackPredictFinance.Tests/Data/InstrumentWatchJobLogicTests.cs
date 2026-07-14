using BackPredictFinance.Common.enums;
using BackPredictFinance.Datas.Context;
using BackPredictFinance.Datas.Entities;
using BackPredictFinance.Services.ClientFinanceServices.Analysis;
using BackPredictFinance.Services.Notifications;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace BackPredictFinance.Tests.Data;

public sealed class InstrumentWatchJobLogicTests
{
    private static FinanceDbContext BuildInMemoryDb(string name)
    {
        var options = new DbContextOptionsBuilder<FinanceDbContext>()
            .UseInMemoryDatabase(name)
            .Options;

        var httpContextAccessorMock = new Mock<IHttpContextAccessor>();
        httpContextAccessorMock.Setup(x => x.HttpContext).Returns((HttpContext?)null);

        return new FinanceDbContext(options, httpContextAccessorMock.Object);
    }

    private static User BuildUser(string userId) => new()
    {
        Id = userId,
        UserName = $"{userId}@test.local",
        Email = $"{userId}@test.local",
        NormalizedEmail = $"{userId}@test.local".ToUpperInvariant(),
        NormalizedUserName = $"{userId}@test.local".ToUpperInvariant(),
        AlertDataStaleEnabled = true,
        AlertPatternStateChangeEnabled = true,
        AlertLevelCrossedEnabled = true,
        IsActive = true
    };

    private static Asset BuildAsset(string assetId) => new()
    {
        Id = assetId,
        Symbol = assetId,
        ProviderSymbol = assetId,
        Exchange = "XPAR",
        Currency = "EUR",
        AssetType = AssetTypeEnum.Stock
    };

    private static UserAsset BuildUserAsset(string userId, string assetId) => new()
    {
        Id = $"{userId}-{assetId}",
        UserId = userId,
        AssetId = assetId,
        Quantity = 10m
    };

    [Fact]
    public async Task ProcessDataStale_WhenLastCandleIsOlderThan4TradingDays_EmitsDataStaleNotification()
    {
        var db = BuildInMemoryDb($"watch-stale-{Guid.NewGuid():N}");
        var userId = "user-watch-1";
        var assetId = "asset-watch-1";

        var now = new DateTime(2026, 6, 10, 10, 0, 0, DateTimeKind.Utc);
        var staleCandle = new DateTime(2026, 6, 3, 0, 0, 0, DateTimeKind.Utc);

        db.Users.Add(BuildUser(userId));
        db.Assets.Add(BuildAsset(assetId));
        db.UserAssets.Add(BuildUserAsset(userId, assetId));
        db.AssetCandleSnapshots.Add(new AssetCandleSnapshot
        {
            Id = "candle-stale-1",
            AssetId = assetId,
            TimestampUtc = staleCandle,
            Interval = "1d",
            Open = 100m, High = 105m, Low = 98m, Close = 103m, Volume = 500m,
            Source = "test"
        });
        await db.SaveChangesAsync();

        var emitter = new ProactiveAlertEmitter(NullLogger<ProactiveAlertEmitter>.Instance);

        await InvokeProcessDataStaleAsync(db, emitter, userId, assetId, now);

        var notifications = await db.UserNotifications
            .AsNoTracking()
            .Where(n => n.UserId == userId && n.AlertTrigger == AlertTrigger.DataStale)
            .ToListAsync();

        Assert.Single(notifications);
        Assert.Equal(assetId, notifications[0].TargetEntityId);
        Assert.Equal(NotificationTargetScreenEnum.InstrumentDetail, notifications[0].TargetScreen);
    }

    [Fact]
    public async Task ProcessDataStale_WhenCalledTwiceSameDay_DoesNotCreateDuplicate()
    {
        var db = BuildInMemoryDb($"watch-stale-dedup-{Guid.NewGuid():N}");
        var userId = "user-watch-2";
        var assetId = "asset-watch-2";

        var now = new DateTime(2026, 6, 10, 10, 0, 0, DateTimeKind.Utc);
        var staleCandle = new DateTime(2026, 6, 3, 0, 0, 0, DateTimeKind.Utc);

        db.Users.Add(BuildUser(userId));
        db.Assets.Add(BuildAsset(assetId));
        db.UserAssets.Add(BuildUserAsset(userId, assetId));
        db.AssetCandleSnapshots.Add(new AssetCandleSnapshot
        {
            Id = "candle-dedup-1",
            AssetId = assetId,
            TimestampUtc = staleCandle,
            Interval = "1d",
            Open = 100m, High = 105m, Low = 98m, Close = 103m, Volume = 500m,
            Source = "test"
        });
        await db.SaveChangesAsync();

        var emitter = new ProactiveAlertEmitter(NullLogger<ProactiveAlertEmitter>.Instance);

        await InvokeProcessDataStaleAsync(db, emitter, userId, assetId, now);
        await InvokeProcessDataStaleAsync(db, emitter, userId, assetId, now);

        var count = await db.UserNotifications
            .AsNoTracking()
            .CountAsync(n => n.UserId == userId && n.AlertTrigger == AlertTrigger.DataStale);

        Assert.Equal(1, count);
    }

    [Fact]
    public async Task ProcessDataStale_WhenCandleIsFresh_DoesNotEmit()
    {
        var db = BuildInMemoryDb($"watch-fresh-{Guid.NewGuid():N}");
        var userId = "user-watch-3";
        var assetId = "asset-watch-3";

        var now = new DateTime(2026, 6, 10, 10, 0, 0, DateTimeKind.Utc);
        var freshCandle = new DateTime(2026, 6, 9, 0, 0, 0, DateTimeKind.Utc);

        db.Users.Add(BuildUser(userId));
        db.Assets.Add(BuildAsset(assetId));
        db.UserAssets.Add(BuildUserAsset(userId, assetId));
        db.AssetCandleSnapshots.Add(new AssetCandleSnapshot
        {
            Id = "candle-fresh-1",
            AssetId = assetId,
            TimestampUtc = freshCandle,
            Interval = "1d",
            Open = 100m, High = 105m, Low = 98m, Close = 103m, Volume = 500m,
            Source = "test"
        });
        await db.SaveChangesAsync();

        var emitter = new ProactiveAlertEmitter(NullLogger<ProactiveAlertEmitter>.Instance);

        await InvokeProcessDataStaleAsync(db, emitter, userId, assetId, now);

        var count = await db.UserNotifications
            .AsNoTracking()
            .CountAsync(n => n.UserId == userId && n.AlertTrigger == AlertTrigger.DataStale);

        Assert.Equal(0, count);
    }

    private static async Task InvokeProcessDataStaleAsync(
        FinanceDbContext db,
        IProactiveAlertEmitter emitter,
        string userId,
        string assetId,
        DateTime now,
        CancellationToken ct = default)
    {
        var latestCandle = await db.AssetCandleSnapshots
            .AsNoTracking()
            .Where(c => c.AssetId == assetId && c.Interval == "1d")
            .OrderByDescending(c => c.TimestampUtc)
            .Select(c => (DateTime?)c.TimestampUtc)
            .FirstOrDefaultAsync(ct);

        var freshness = FreshnessClassifier.Classify(latestCandle, now);

        if (freshness != FreshnessStatusEnum.Stale)
        {
            return;
        }

        await emitter.EmitAsync(
            db,
            userId,
            AlertTrigger.DataStale,
            NotificationTargetScreenEnum.InstrumentDetail,
            assetId,
            now,
            "Donnees de marche obsoletes",
            "Les donnees de marche de cet instrument n'ont pas ete actualisees depuis plusieurs jours de bourse.",
            ct);
    }
}
