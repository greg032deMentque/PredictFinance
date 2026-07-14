namespace BackPredictFinance.ViewModels.UserViewModels
{
    public sealed class AdminUserUpdateRequestViewModel
    {
        public string UserId { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Password { get; set; }
        public string? Role { get; set; } = "User";
        public bool? IsActive { get; set; }
        public string? PhoneNumber { get; set; }
    }
}
