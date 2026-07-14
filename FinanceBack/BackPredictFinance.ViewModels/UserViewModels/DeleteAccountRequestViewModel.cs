using System.ComponentModel.DataAnnotations;

namespace BackPredictFinance.ViewModels.UserViewModels
{
    public sealed class DeleteAccountRequestViewModel
    {
        [Required]
        public string CurrentPassword { get; set; } = string.Empty;

        [Required]
        public bool ConfirmDeletion { get; set; }
    }
}
