using System.ComponentModel.DataAnnotations;

namespace BackPredictFinance.ViewModels.AdminViewModels.ScoringPolicy
{
    public sealed class AdminScoringPolicyVersionCreateRequestViewModel : IValidatableObject
    {
        [Required]
        [MaxLength(160)]
        public string DisplayName { get; set; } = string.Empty;

        [Range(1, 6)]
        public int MinimumCategoriesRequiredFloor { get; set; } = 1;

        [Range(1, 6)]
        public int MinimumCategoriesRequiredCeiling { get; set; } = 6;

        [Range(1, 6)]
        public int MinimumCategoriesRequiredDefault { get; set; } = 3;

        [Range(1, 1000)]
        public int MinimumSectorSampleSize { get; set; } = 5;

        public bool CoveragePenaltySupported { get; set; } = true;

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (MinimumCategoriesRequiredFloor > MinimumCategoriesRequiredCeiling)
            {
                yield return new ValidationResult(
                    "MinimumCategoriesRequiredFloor must be less than or equal to MinimumCategoriesRequiredCeiling.",
                    [nameof(MinimumCategoriesRequiredFloor), nameof(MinimumCategoriesRequiredCeiling)]);
            }

            if (MinimumCategoriesRequiredDefault < MinimumCategoriesRequiredFloor || MinimumCategoriesRequiredDefault > MinimumCategoriesRequiredCeiling)
            {
                yield return new ValidationResult(
                    "MinimumCategoriesRequiredDefault must be between MinimumCategoriesRequiredFloor and MinimumCategoriesRequiredCeiling.",
                    [nameof(MinimumCategoriesRequiredDefault)]);
            }
        }
    }
}
