using System.ComponentModel.DataAnnotations;

namespace BackPredictFinance.ViewModels.AdminViewModels.Content
{
    public sealed class LegalCardUpsertRequestViewModel
    {
        [Required]
        [MaxLength(64)]
        public string Key { get; set; } = string.Empty;

        [Required]
        [MaxLength(64)]
        public string Icon { get; set; } = string.Empty;

        [Required]
        [MaxLength(256)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [MaxLength(1024)]
        public string Description { get; set; } = string.Empty;

        public DateTime? EffectiveDate { get; set; }

        [MaxLength(256)]
        public string? TargetRoute { get; set; }

        public int DisplayOrder { get; set; }

        public bool IsPublished { get; set; }
    }
}
