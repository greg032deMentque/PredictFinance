using BackPredictFinance.Common.enums;
using BackPredictFinance.Datas.Context;
using BackPredictFinance.Datas.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BackPredictFinance.Services.Notifications
{
    public interface IProactiveAlertEmitter
    {
        Task EmitAsync(
            FinanceDbContext db,
            string userId,
            AlertTrigger trigger,
            NotificationTargetScreenEnum targetScreen,
            string? targetEntityId,
            DateTime dayKeyUtc,
            string title,
            string summary,
            CancellationToken ct = default);
    }

    public sealed class ProactiveAlertEmitter : IProactiveAlertEmitter
    {
        private readonly ILogger<ProactiveAlertEmitter> _logger;

        public ProactiveAlertEmitter(ILogger<ProactiveAlertEmitter> logger)
        {
            _logger = logger;
        }

        public async Task EmitAsync(
            FinanceDbContext db,
            string userId,
            AlertTrigger trigger,
            NotificationTargetScreenEnum targetScreen,
            string? targetEntityId,
            DateTime dayKeyUtc,
            string title,
            string summary,
            CancellationToken ct = default)
        {
            var user = await db.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == userId, ct);

            if (user is null)
            {
                _logger.LogWarning("ProactiveAlertEmitter: user {UserId} not found — skip", userId);
                return;
            }

            if (!IsPreferenceEnabled(user, trigger))
            {
                _logger.LogDebug("ProactiveAlertEmitter: trigger {Trigger} disabled for user {UserId} — skip", trigger, userId);
                return;
            }

            var normalizedDayKey = dayKeyUtc.Date;
            var alreadyExists = await db.UserNotifications
                .AsNoTracking()
                .AnyAsync(n =>
                    n.UserId == userId
                    && n.TargetEntityId == targetEntityId
                    && n.AlertTrigger == trigger
                    && n.AlertDayKeyUtc == normalizedDayKey,
                    ct);

            if (alreadyExists)
            {
                _logger.LogDebug(
                    "ProactiveAlertEmitter: notification already exists for user={UserId} trigger={Trigger} entity={EntityId} day={Day} — skip",
                    userId, trigger, targetEntityId, normalizedDayKey);
                return;
            }

            var notification = new UserNotification
            {
                NotificationId = Guid.NewGuid().ToString("N"),
                UserId = userId,
                Category = NotificationCategoryEnum.Analysis,
                Status = NotificationStatusEnum.Unread,
                Title = title,
                Summary = summary,
                TargetScreen = targetScreen,
                TargetEntityId = targetEntityId,
                AlertTrigger = trigger,
                AlertDayKeyUtc = normalizedDayKey
            };

            try
            {
                await db.UserNotifications.AddAsync(notification, ct);
                await db.SaveChangesAsync(ct);

                _logger.LogInformation(
                    "ProactiveAlertEmitter: alerte {Trigger} emise pour user={UserId} entity={EntityId}",
                    trigger, userId, targetEntityId);
            }
            catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
            {
                _logger.LogDebug(
                    "ProactiveAlertEmitter: contrainte unique sur trigger={Trigger} user={UserId} entity={EntityId} — no-op",
                    trigger, userId, targetEntityId);

                db.UserNotifications.Remove(notification);
            }
        }

        private static bool IsPreferenceEnabled(Datas.Entities.User user, AlertTrigger trigger)
        {
            return trigger switch
            {
                AlertTrigger.PatternStateChange => user.AlertPatternStateChangeEnabled,
                AlertTrigger.LevelCrossed => user.AlertLevelCrossedEnabled,
                AlertTrigger.DataStale => user.AlertDataStaleEnabled,
                _ => false
            };
        }

        private static bool IsUniqueConstraintViolation(DbUpdateException ex)
        {
            return ex.InnerException?.Message.Contains("UNIQUE", StringComparison.OrdinalIgnoreCase) == true
                || ex.InnerException?.Message.Contains("unique constraint", StringComparison.OrdinalIgnoreCase) == true;
        }
    }
}
