using System.ComponentModel.DataAnnotations;

namespace BackPredictFinance.ViewModels.AdminViewModels.Content
{
    public sealed class AnalysisConceptCreateRequestViewModel
    {
        [Required]
        [MaxLength(100)]
        public string Code { get; set; } = string.Empty;

        [Required]
        [MaxLength(200)]
        public string Label { get; set; } = string.Empty;

        [Required]
        [MaxLength(4000)]
        public string Explanation { get; set; } = string.Empty;
    }
}
