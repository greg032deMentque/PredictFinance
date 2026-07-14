using BackPredictFinance.Common.enums;

namespace BackPredictFinance.ViewModels.NotificationViewModels
{
    public sealed class NotificationItemViewModel
    {
        public string NotificationId { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public NotificationCategoryEnum Category { get; set; }
        public NotificationStatusEnum Status { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Summary { get; set; } = string.Empty;
        public DateTime CreatedAtUtc { get; set; }
        public NotificationTargetScreenEnum? TargetScreen { get; set; }
        public string? TargetEntityId { get; set; }
        public DateTime? ReadAtUtc { get; set; }
        public AlertTrigger? AlertTrigger { get; set; }
    }
}
