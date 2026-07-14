using BackPredictFinance.Common.enums;
using BackPredictFinance.ViewModels.NotificationViewModels;
using Microsoft.EntityFrameworkCore;

namespace BackPredictFinance.Services.Notifications
{
    public interface INotificationCenterService
    {
        /// <summary>
        /// Retourne les notifications du user courant avec filtrage optionnel par categorie et statut.
        /// </summary>
        Task<List<NotificationItemViewModel>> GetListAsync(NotificationCategoryEnum? category, NotificationStatusEnum? status, int take = 50, CancellationToken ct = default);

        /// <summary>
        /// Marque une notification du user courant comme lue et retourne l'etat mis a jour.
        /// </summary>
        Task<NotificationItemViewModel> MarkAsReadAsync(string notificationId, CancellationToken ct = default);
    }

    public sealed class NotificationCenterService : BaseService, INotificationCenterService
    {
        private const int DefaultTake = 50;
        private const int MaxTake = 200;

        public NotificationCenterService(IServiceProvider serviceProvider) : base(serviceProvider)
        {
        }

        public async Task<List<NotificationItemViewModel>> GetListAsync(NotificationCategoryEnum? category, NotificationStatusEnum? status, int take = 50, CancellationToken ct = default)
        {
            var currentUserId = GetRequiredCurrentUserId();
            var normalizedTake = NormalizeTake(take);

            var query = _financeDbContext.UserNotifications
                .AsNoTracking()
                .Where(x => x.UserId == currentUserId);

            if (category.HasValue)
            {
                query = query.Where(x => x.Category == category.Value);
            }

            if (status.HasValue)
            {
                query = query.Where(x => x.Status == status.Value);
            }

            return await query
                .OrderByDescending(x => x.CreatedAtUtc)
                .ThenBy(x => x.NotificationId)
                .Take(normalizedTake)
                .Select(MapNotification())
                .ToListAsync(ct);
        }

        public async Task<NotificationItemViewModel> MarkAsReadAsync(string notificationId, CancellationToken ct = default)
        {
            var currentUserId = GetRequiredCurrentUserId();
            var normalizedNotificationId = (notificationId ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(normalizedNotificationId))
            {
                throw new ArgumentException("Notification id is required.", nameof(notificationId));
            }

            var notification = await _financeDbContext.UserNotifications
                .FirstOrDefaultAsync(x => x.UserId == currentUserId && x.NotificationId == normalizedNotificationId, ct);

            if (notification is null)
            {
                throw new KeyNotFoundException($"Notification '{normalizedNotificationId}' was not found for the current user.");
            }

            if (notification.Status != NotificationStatusEnum.Read)
            {
                notification.Status = NotificationStatusEnum.Read;
                notification.ReadAtUtc = DateTime.UtcNow;
                await _financeDbContext.SaveChangesAsync(ct);
            }

            return MapNotificationEntity(notification);
        }

        private string GetRequiredCurrentUserId()
        {
            if (!string.IsNullOrWhiteSpace(_currentUserId))
            {
                return _currentUserId;
            }

            throw new InvalidOperationException("No current user is available for the notification center.");
        }

        private static int NormalizeTake(int take)
        {
            if (take <= 0)
            {
                return DefaultTake;
            }

            return take > MaxTake ? MaxTake : take;
        }

        private static System.Linq.Expressions.Expression<Func<Datas.Entities.UserNotification, NotificationItemViewModel>> MapNotification()
        {
            return notification => new NotificationItemViewModel
            {
                NotificationId = notification.NotificationId,
                UserId = notification.UserId,
                Category = notification.Category,
                Status = notification.Status,
                Title = notification.Title,
                Summary = notification.Summary,
                CreatedAtUtc = notification.CreatedAtUtc,
                TargetScreen = notification.TargetScreen,
                TargetEntityId = notification.TargetEntityId,
                ReadAtUtc = notification.ReadAtUtc,
                AlertTrigger = notification.AlertTrigger
            };
        }

        private static NotificationItemViewModel MapNotificationEntity(Datas.Entities.UserNotification notification)
        {
            return new NotificationItemViewModel
            {
                NotificationId = notification.NotificationId,
                UserId = notification.UserId,
                Category = notification.Category,
                Status = notification.Status,
                Title = notification.Title,
                Summary = notification.Summary,
                CreatedAtUtc = notification.CreatedAtUtc,
                TargetScreen = notification.TargetScreen,
                TargetEntityId = notification.TargetEntityId,
                ReadAtUtc = notification.ReadAtUtc,
                AlertTrigger = notification.AlertTrigger
            };
        }
    }
}
