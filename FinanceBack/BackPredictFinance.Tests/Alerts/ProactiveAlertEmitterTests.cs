using BackPredictFinance.Common.enums;
using BackPredictFinance.Datas.Context;
using BackPredictFinance.Datas.Entities;
using BackPredictFinance.Services.Notifications;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace BackPredictFinance.Tests.Alerts;

public sealed class ProactiveAlertEmitterTests
{
    private static FinanceDbContext BuildInMemoryDb()
    {
        var options = new DbContextOptionsBuilder<FinanceDbContext>()
            .UseInMemoryDatabase($"emitter-test-{Guid.NewGuid():N}")
            .Options;

        var httpContextAccessorMock = new Mock<IHttpContextAccessor>();
        httpContextAccessorMock.Setup(x => x.HttpContext).Returns((HttpContext?)null);

        return new FinanceDbContext(options, httpContextAccessorMock.Object);
    }

    private static User BuildUser(string userId, bool patternEnabled = true, bool levelEnabled = true, bool staleEnabled = true)
    {
        return new User
        {
            Id = userId,
            UserName = $"{userId}@test.local",
            Email = $"{userId}@test.local",
            NormalizedEmail = $"{userId}@test.local".ToUpperInvariant(),
            NormalizedUserName = $"{userId}@test.local".ToUpperInvariant(),
            AlertPatternStateChangeEnabled = patternEnabled,
            AlertLevelCrossedEnabled = levelEnabled,
            AlertDataStaleEnabled = staleEnabled,
            IsActive = true
        };
    }

    [Fact]
    public async Task EmitAsync_FirstCall_CreatesNotification()
    {
        var db = BuildInMemoryDb();
        var userId = "user-emit-1";
        db.Users.Add(BuildUser(userId));
        await db.SaveChangesAsync();

        var emitter = new ProactiveAlertEmitter(NullLogger<ProactiveAlertEmitter>.Instance);
        var dayKey = new DateTime(2026, 6, 9, 0, 0, 0, DateTimeKind.Utc);

        await emitter.EmitAsync(db, userId, AlertTrigger.DataStale, NotificationTargetScreenEnum.InstrumentDetail, "asset-1", dayKey, "Titre", "Résumé");

        var notifications = await db.UserNotifications.ToListAsync();
        Assert.Single(notifications);
        Assert.Equal(AlertTrigger.DataStale, notifications[0].AlertTrigger);
        Assert.Equal("asset-1", notifications[0].TargetEntityId);
        Assert.Equal(dayKey.Date, notifications[0].AlertDayKeyUtc);
    }

    [Fact]
    public async Task EmitAsync_SecondCallSameDayAndTrigger_IsNoOp()
    {
        var db = BuildInMemoryDb();
        var userId = "user-emit-2";
        db.Users.Add(BuildUser(userId));
        await db.SaveChangesAsync();

        var emitter = new ProactiveAlertEmitter(NullLogger<ProactiveAlertEmitter>.Instance);
        var dayKey = new DateTime(2026, 6, 9, 0, 0, 0, DateTimeKind.Utc);

        await emitter.EmitAsync(db, userId, AlertTrigger.PatternStateChange, NotificationTargetScreenEnum.AnalysisResult, "asset-1", dayKey, "Titre", "Résumé");
        await emitter.EmitAsync(db, userId, AlertTrigger.PatternStateChange, NotificationTargetScreenEnum.AnalysisResult, "asset-1", dayKey, "Titre", "Résumé");

        var count = await db.UserNotifications.CountAsync();
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task EmitAsync_WhenPreferenceDisabled_SkipsEmission()
    {
        var db = BuildInMemoryDb();
        var userId = "user-emit-3";
        db.Users.Add(BuildUser(userId, levelEnabled: false));
        await db.SaveChangesAsync();

        var emitter = new ProactiveAlertEmitter(NullLogger<ProactiveAlertEmitter>.Instance);
        var dayKey = new DateTime(2026, 6, 9, 0, 0, 0, DateTimeKind.Utc);

        await emitter.EmitAsync(db, userId, AlertTrigger.LevelCrossed, NotificationTargetScreenEnum.InstrumentDetail, "asset-1", dayKey, "Titre", "Résumé");

        var count = await db.UserNotifications.CountAsync();
        Assert.Equal(0, count);
    }

    [Fact]
    public async Task EmitAsync_DifferentDayAndSameTrigger_CreatesTwoNotifications()
    {
        var db = BuildInMemoryDb();
        var userId = "user-emit-4";
        db.Users.Add(BuildUser(userId));
        await db.SaveChangesAsync();

        var emitter = new ProactiveAlertEmitter(NullLogger<ProactiveAlertEmitter>.Instance);
        var day1 = new DateTime(2026, 6, 9, 0, 0, 0, DateTimeKind.Utc);
        var day2 = new DateTime(2026, 6, 10, 0, 0, 0, DateTimeKind.Utc);

        await emitter.EmitAsync(db, userId, AlertTrigger.DataStale, NotificationTargetScreenEnum.InstrumentDetail, "asset-1", day1, "Titre", "Résumé");
        await emitter.EmitAsync(db, userId, AlertTrigger.DataStale, NotificationTargetScreenEnum.InstrumentDetail, "asset-1", day2, "Titre", "Résumé");

        var count = await db.UserNotifications.CountAsync();
        Assert.Equal(2, count);
    }
}
