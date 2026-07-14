using System.ComponentModel.DataAnnotations;

namespace BackPredictFinance.ViewModels.UserViewModels.AuthViewModels
{
    public sealed class ConfirmEmailViewModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Token { get; set; } = string.Empty;
    }
}
