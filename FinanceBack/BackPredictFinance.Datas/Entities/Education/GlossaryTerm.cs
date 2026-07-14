using BackPredictFinance.Common.enums;

namespace BackPredictFinance.Datas.Entities
{
    /// <summary>
    /// Terme du glossaire produits (PEA, PER, PEL, assurance vie, général).
    /// </summary>
    public sealed class GlossaryTerm : AuditableEntityBase
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Term { get; set; } = string.Empty;
        public string NormalizedTerm { get; set; } = string.Empty;
        public string Definition { get; set; } = string.Empty;
        public GlossaryTermEnum Category { get; set; }
        public bool IsActive { get; set; }
        public bool IsPublished { get; set; }
        public bool IsDeleted { get; set; }
    }
}