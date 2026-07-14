using BackPredictFinance.Common.enums;

namespace BackPredictFinance.Datas.Entities
{
    /// <summary>
    /// Persiste un item du centre de notifications V1 avec son etat de lecture et sa cible de routage.
    /// </summary>
    public sealed class UserNotification : AuditableEntityBase
    {
        public string NotificationId { get; set; } = Guid.NewGuid().ToString();
        public string UserId { get; set; } = string.Empty;
        public User User { get; set; } = null!;
        public NotificationCategoryEnum Category { get; set; } = NotificationCategoryEnum.Analysis;
        public NotificationStatusEnum Status { get; set; } = NotificationStatusEnum.Unread;
        public string Title { get; set; } = string.Empty;
        public string Summary { get; set; } = string.Empty;
        public NotificationTargetScreenEnum? TargetScreen { get; set; }
        public string? TargetEntityId { get; set; }
        public DateTime? ReadAtUtc { get; set; }
        public AlertTrigger? AlertTrigger { get; set; }
        public DateTime? AlertDayKeyUtc { get; set; }
    }
}
