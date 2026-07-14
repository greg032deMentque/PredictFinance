using BackPredictFinance.Common.enums;
using System.ComponentModel.DataAnnotations;

namespace BackPredictFinance.ViewModels.AdminViewModels.Education
{
    public sealed class GlossaryTermUpsertRequestViewModel
    {
        [Required]
        [MaxLength(256)]
        public string Term { get; set; } = string.Empty;

        [Required]
        [MaxLength(2048)]
        public string Definition { get; set; } = string.Empty;

        [Required]
        public GlossaryTermEnum Category { get; set; }

        public bool IsPublished { get; set; }
    }
}
