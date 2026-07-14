using BackPredictFinance.Common.enums;
using System.ComponentModel.DataAnnotations;

namespace BackPredictFinance.ViewModels.AdminViewModels.Education
{
    public sealed class EducationArticleUpsertRequestViewModel
    {
        [Required]
        [MaxLength(128)]
        public string Slug { get; set; } = string.Empty;

        [Required]
        public EducationProductTypeEnum ProductType { get; set; }

        [Required]
        [MaxLength(256)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [MaxLength(1024)]
        public string Summary { get; set; } = string.Empty;

        [Required]
        [MaxLength(16000)]
        public string BodyMarkdown { get; set; } = string.Empty;

        public int DisplayOrder { get; set; }

        public bool IsPublished { get; set; }
    }
}
