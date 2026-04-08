using System.Security.Claims;

namespace BackPredictFinance.Common.Jwt
{
    public class Messages
    {
    }

    public static class ClaimsPrincipalExtensions
    {
        public static string? GetUserId(this ClaimsPrincipal user)
        {
            return user?.FindFirst("sub")?.Value
                ?? user?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        }
    }
}
