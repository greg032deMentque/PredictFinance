namespace BackPredictFinance.Common.Auth
{
    public sealed record RefreshTokenResult(string Token, DateTime ExpiresUtc);
}
