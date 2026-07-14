namespace BackPredictFinance.Datas.Entities
{
    public sealed class LearnTopic : AuditableEntityBase
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string TopicId { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Summary { get; set; } = string.Empty;
        public string RoutePath { get; set; } = string.Empty;
        public int DisplayOrder { get; set; }
        public bool IsActive { get; set; }
        public bool IsPublished { get; set; }
        public bool IsDeleted { get; set; }
    }
}
