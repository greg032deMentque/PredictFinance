namespace BackPredictFinance.Datas.Entities
{
    public sealed class LegalCard : AuditableEntityBase
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Key { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime? EffectiveDate { get; set; }
        public string? TargetRoute { get; set; }
        public int DisplayOrder { get; set; }
        public bool IsActive { get; set; }
        public bool IsPublished { get; set; }
        public bool IsDeleted { get; set; }
    }
}
