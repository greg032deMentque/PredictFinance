using Microsoft.IdentityModel.JsonWebTokens;
using System.Security.Claims;

namespace BackPredictFinance.Contracts.Common
{
    public class Messages
    {
    }

    public static class ClaimsPrincipalExtensions
    {
        public static string? GetUserId(this ClaimsPrincipal user)
        {
            return user?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                ?? user?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        }
    }
}
