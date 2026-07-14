using System.Security.Claims;

namespace BackPredictFinance.Common.Security
{
    public static class ClaimsPrincipalExtensions
    {
        private const string SubjectClaimType = "sub";

        public static string? GetUserId(this ClaimsPrincipal user)
        {
            return user?.FindFirst(SubjectClaimType)?.Value
                ?? user?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        }
    }
}
