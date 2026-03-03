namespace BackPredictFinance.ViewModels.UserViewModels.AuthViewModels
{
    public class TokenViewModel
    {
        public string Token { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }

        public string RefreshToken { get; set; }
        public bool IsFirstConnection { get; set; }
        public DateTime RefreshTokenExpiresAtUtc { get; set; }
    }
}