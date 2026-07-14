using BackPredictFinance.Common.enums;

namespace BackPredictFinance.Datas.Entities
{
    /// <summary>
    /// Article éducatif pédagogique associé à un type de produit financier.
    /// </summary>
    public sealed class EducationArticle : AuditableEntityBase
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Slug { get; set; } = string.Empty;
        public EducationProductTypeEnum ProductType { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Summary { get; set; } = string.Empty;
        public string BodyMarkdown { get; set; } = string.Empty;
        public int DisplayOrder { get; set; }
        public bool IsActive { get; set; }
        public bool IsPublished { get; set; }
        public bool IsDeleted { get; set; }
    }
}
