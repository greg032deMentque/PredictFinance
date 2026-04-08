namespace BackPredictFinance.Common.Jwt
{
    public sealed record RefreshTokenResult(string Token, DateTime ExpiresUtc);
}
