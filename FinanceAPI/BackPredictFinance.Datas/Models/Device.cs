namespace BackPredictFinance.Datas.Models
{
    public class Device : AuditableEntityBase
    {
        public string Id { get; set; }
        public string UserId { get; set; }
        public User User { get; set; }
        public string? MobileId { get; set; }
        public string? PushTokenMobile { get; set; }
        public string? PushTokenWeb { get; set; }

        public bool IsIos { get; set; }
    }
}