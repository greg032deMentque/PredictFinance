namespace BackPredictFinance.Contracts.Auth
{
    public sealed record RefreshTokenResult(string Token, DateTime ExpiresUtc);
}
