using BackPredictFinance.Common.enums;
using BackPredictFinance.Datas.Context;
using BackPredictFinance.Datas.Entities;
using BackPredictFinance.Services.Notifications;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace BackPredictFinance.Tests.Alerts;

public sealed class LevelCrossedAlertTests
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

    private static User BuildUser(string userId)
    {
        return new User
        {
            Id = userId,
            UserName = $"{userId}@test.local",
            Email = $"{userId}@test.local",
            NormalizedEmail = $"{userId}@test.local".ToUpperInvariant(),
            NormalizedUserName = $"{userId}@test.local".ToUpperInvariant(),
            AlertLevelCrossedEnabled = true,
            IsActive = true
        };
    }

    [Fact]
    public async Task LevelCrossed_WhenTargetHit_EmitsSingleAlertToRunOwner()
    {
        var db = BuildInMemoryDb($"level-crossed-{Guid.NewGuid():N}");
        var userId = "user-level-1";
        var assetId = "asset-level-1";
        db.Users.Add(BuildUser(userId));
        await db.SaveChangesAsync();

        var emitter = new ProactiveAlertEmitter(NullLogger<ProactiveAlertEmitter>.Instance);
        var dayKey = new DateTime(2026, 6, 9, 0, 0, 0, DateTimeKind.Utc);

        await emitter.EmitAsync(
            db,
            userId,
            AlertTrigger.LevelCrossed,
            NotificationTargetScreenEnum.InstrumentDetail,
            assetId,
            dayKey,
            "Objectif de cours atteint",
            "Un niveau cible que vous suiviez vient d'etre franchi. Consultez la fiche instrument pour en savoir plus.");

        var notifications = await db.UserNotifications
            .Where(n => n.UserId == userId && n.AlertTrigger == AlertTrigger.LevelCrossed)
            .ToListAsync();

        Assert.Single(notifications);
        Assert.Equal(assetId, notifications[0].TargetEntityId);
        Assert.Equal(NotificationTargetScreenEnum.InstrumentDetail, notifications[0].TargetScreen);
    }

    [Fact]
    public async Task LevelCrossed_WhenEmittedTwiceSameDay_OnlyOneNotificationCreated()
    {
        var db = BuildInMemoryDb($"level-crossed-dedup-{Guid.NewGuid():N}");
        var userId = "user-level-2";
        var assetId = "asset-level-2";
        db.Users.Add(BuildUser(userId));
        await db.SaveChangesAsync();

        var emitter = new ProactiveAlertEmitter(NullLogger<ProactiveAlertEmitter>.Instance);
        var dayKey = new DateTime(2026, 6, 9, 0, 0, 0, DateTimeKind.Utc);

        await emitter.EmitAsync(db, userId, AlertTrigger.LevelCrossed, NotificationTargetScreenEnum.InstrumentDetail, assetId, dayKey, "Titre", "Résumé");
        await emitter.EmitAsync(db, userId, AlertTrigger.LevelCrossed, NotificationTargetScreenEnum.InstrumentDetail, assetId, dayKey, "Titre", "Résumé");

        var count = await db.UserNotifications.CountAsync(n => n.AlertTrigger == AlertTrigger.LevelCrossed);
        Assert.Equal(1, count);
    }
}
