namespace BackPredictFinance.ViewModels.UserViewModels.AuthViewModels
{
    public sealed class PublicSignupResponseViewModel
    {
        public string Email { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public bool CanLogin { get; set; }
        public bool RequiresEmailConfirmation { get; set; }
    }
}
