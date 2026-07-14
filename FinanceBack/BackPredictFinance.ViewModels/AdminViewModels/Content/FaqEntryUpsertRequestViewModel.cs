using System.ComponentModel.DataAnnotations;

namespace BackPredictFinance.ViewModels.AdminViewModels.Content
{
    public sealed class FaqEntryUpsertRequestViewModel
    {
        [Required]
        [MaxLength(128)]
        public string Category { get; set; } = string.Empty;

        [Required]
        [MaxLength(512)]
        public string Question { get; set; } = string.Empty;

        [Required]
        [MaxLength(2048)]
        public string Answer { get; set; } = string.Empty;

        public int DisplayOrder { get; set; }

        public bool IsPublished { get; set; }
    }
}
