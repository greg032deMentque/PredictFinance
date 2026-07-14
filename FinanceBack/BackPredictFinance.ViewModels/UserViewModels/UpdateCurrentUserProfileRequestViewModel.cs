namespace BackPredictFinance.ViewModels.UserViewModels
{
    public sealed class UpdateCurrentUserProfileRequestViewModel
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
    }
}
