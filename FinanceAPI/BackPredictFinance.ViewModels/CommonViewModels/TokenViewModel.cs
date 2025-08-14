namespace BackPredictFinance.ViewModels.CommonViewModels
{
    public class TokenViewModel
    {
        public string Token { get; set; }
        public string? Firstname { get; set; }
        public string? Lastname { get; set; }
        public string RefreshToken { get; set; }
        public bool IsFirstConnection { get; set; }
    }
}