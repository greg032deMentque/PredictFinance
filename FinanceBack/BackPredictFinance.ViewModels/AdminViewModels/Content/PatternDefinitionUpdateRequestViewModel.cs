using System.ComponentModel.DataAnnotations;

namespace BackPredictFinance.ViewModels.AdminViewModels.Content
{
    public sealed class PatternDefinitionUpdateRequestViewModel
    {
        [Required]
        [MaxLength(200)]
        public string DisplayName { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string Family { get; set; } = string.Empty;

        [Required]
        [MaxLength(200)]
        public string FamilyLabel { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string Direction { get; set; } = string.Empty;

        [Required]
        [MaxLength(200)]
        public string DirectionLabel { get; set; } = string.Empty;

        [Required]
        [MaxLength(2000)]
        public string Description { get; set; } = string.Empty;

        [Required]
        [MaxLength(2000)]
        public string AnalysisNarrative { get; set; } = string.Empty;

        [Range(0, 1)]
        public decimal Reliability { get; set; }

        [Required]
        [MaxLength(100)]
        public string ReliabilityLabel { get; set; } = string.Empty;
    }
}
