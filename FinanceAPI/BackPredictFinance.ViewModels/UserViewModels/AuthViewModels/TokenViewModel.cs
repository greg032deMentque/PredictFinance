namespace BackPredictFinance.ViewModels.UserViewModels.AuthViewModels
{
    public class TokenViewModel
    {
        public string Token { get; set; } = string.Empty;
        public string? FirstName { get; set; }
        public string? LastName { get; set; }

        public string RefreshToken { get; set; } = string.Empty;
        public bool IsFirstConnection { get; set; }
        public DateTime RefreshTokenExpiresAtUtc { get; set; }
    }
}
