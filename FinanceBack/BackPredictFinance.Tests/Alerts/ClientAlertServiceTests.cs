using BackPredictFinance.Common.enums;
using BackPredictFinance.Datas.Context;
using BackPredictFinance.Datas.Entities;
using BackPredictFinance.Services.Notifications;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace BackPredictFinance.Tests.Alerts;

public sealed class ClientAlertServiceTests
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

    private static User BuildUser(string userId, bool levelEnabled = true)
    {
        return new User
        {
            Id = userId,
            UserName = $"{userId}@test.local",
            Email = $"{userId}@test.local",
            NormalizedEmail = $"{userId}@test.local".ToUpperInvariant(),
            NormalizedUserName = $"{userId}@test.local".ToUpperInvariant(),
            AlertLevelCrossedEnabled = levelEnabled,
            AlertPatternStateChangeEnabled = true,
            AlertDataStaleEnabled = true,
            IsActive = true
        };
    }

    [Fact]
    public async Task EmitAsync_WhenLevelCrossedPreferenceEnabled_CreatesNotification()
    {
        var db = BuildInMemoryDb($"alert-svc-ok-{Guid.NewGuid():N}");
        var userId = "user-alert-enabled";
        var assetId = "asset-alert-1";

        db.Users.Add(BuildUser(userId, levelEnabled: true));
        await db.SaveChangesAsync();

        var emitter = new ProactiveAlertEmitter(NullLogger<ProactiveAlertEmitter>.Instance);
        var dayKey = new DateTime(2026, 6, 10, 0, 0, 0, DateTimeKind.Utc);

        await emitter.EmitAsync(
            db,
            userId,
            AlertTrigger.LevelCrossed,
            NotificationTargetScreenEnum.InstrumentDetail,
            assetId,
            dayKey,
            "Alerte niveau 150,0000 — AIR.PA",
            "Vous serez notifie si AIR.PA franchit le niveau 150,0000.");

        var notifications = await db.UserNotifications
            .Where(n => n.UserId == userId && n.AlertTrigger == AlertTrigger.LevelCrossed)
            .ToListAsync();

        Assert.Single(notifications);
        Assert.Equal(AlertTrigger.LevelCrossed, notifications[0].AlertTrigger);
        Assert.Equal(NotificationTargetScreenEnum.InstrumentDetail, notifications[0].TargetScreen);
        Assert.Equal(assetId, notifications[0].TargetEntityId);
    }

    [Fact]
    public async Task EmitAsync_WhenLevelCrossedPreferenceDisabled_SkipsEmission()
    {
        var db = BuildInMemoryDb($"alert-svc-disabled-{Guid.NewGuid():N}");
        var userId = "user-alert-disabled";
        var assetId = "asset-alert-2";

        db.Users.Add(BuildUser(userId, levelEnabled: false));
        await db.SaveChangesAsync();

        var emitter = new ProactiveAlertEmitter(NullLogger<ProactiveAlertEmitter>.Instance);
        var dayKey = new DateTime(2026, 6, 10, 0, 0, 0, DateTimeKind.Utc);

        await emitter.EmitAsync(
            db,
            userId,
            AlertTrigger.LevelCrossed,
            NotificationTargetScreenEnum.InstrumentDetail,
            assetId,
            dayKey,
            "Alerte niveau 800,0000 — MC.PA",
            "Vous serez notifie si MC.PA franchit le niveau 800,0000.");

        var count = await db.UserNotifications.CountAsync();
        Assert.Equal(0, count);
    }
}
