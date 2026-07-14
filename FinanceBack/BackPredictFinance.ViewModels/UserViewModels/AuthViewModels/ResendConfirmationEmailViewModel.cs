using System.ComponentModel.DataAnnotations;

namespace BackPredictFinance.ViewModels.UserViewModels.AuthViewModels
{
    public sealed class ResendConfirmationEmailViewModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;
    }
}
