using BackPredictFinance.Common.enums;

namespace BackPredictFinance.Services.AuthServices
{
    /// <summary>
    /// Expose la session résolue de l'utilisateur authentifié courant.
    /// </summary>
    public interface ICurrentUserSessionService
    {
        /// <summary>
        /// Retourne les informations de session de l'utilisateur courant, si disponible.
        /// </summary>
        Task<CurrentUserSessionInfo?> GetCurrentAsync(CancellationToken ct = default);
    }

    /// <summary>
    /// Résout les informations de session du compte courant à partir du contexte HTTP.
    /// </summary>
    public sealed class CurrentUserSessionService : BaseService, ICurrentUserSessionService
    {
        public CurrentUserSessionService(IServiceProvider serviceProvider)
            : base(serviceProvider)
        {
        }

        public async Task<CurrentUserSessionInfo?> GetCurrentAsync(CancellationToken ct = default)
        {
            var currentUser = await GetCurrentUserAsync(ct);
            if (currentUser is null)
            {
                return null;
            }

            var resolvedRoles = (await _userManager.GetRolesAsync(currentUser))
                .Select(ParseRole)
                .Where(role => role.HasValue)
                .Select(role => role!.Value)
                .Distinct()
                .OrderBy(role => role)
                .ToList();

            return new CurrentUserSessionInfo
            {
                UserId = currentUser.Id,
                DisplayName = BuildDisplayName(currentUser.FirstName, currentUser.LastName, currentUser.Email, currentUser.Id),
                Email = currentUser.Email ?? string.Empty,
                Roles = resolvedRoles,
                AllowedAreas = ResolveAllowedAreas(resolvedRoles)
            };
        }

        private static UserRoleEnum? ParseRole(string? rawRole)
        {
            if (Enum.TryParse<UserRoleEnum>(rawRole, true, out var parsedRole))
            {
                return parsedRole;
            }

            return null;
        }

        private static List<UserAreaEnum> ResolveAllowedAreas(IReadOnlyCollection<UserRoleEnum> roles)
        {
            var allowedAreas = new List<UserAreaEnum> { UserAreaEnum.User };

            if (roles.Contains(UserRoleEnum.Admin))
            {
                allowedAreas.Add(UserAreaEnum.Admin);
            }

            return allowedAreas;
        }

        private static string BuildDisplayName(string? firstName, string? lastName, string? email, string userId)
        {
            var displayName = $"{firstName} {lastName}".Trim();
            if (!string.IsNullOrWhiteSpace(displayName))
            {
                return displayName;
            }

            if (!string.IsNullOrWhiteSpace(email))
            {
                return email.Trim();
            }

            return userId;
        }
    }

    /// <summary>
    /// Porte les informations de session projetées pour l'utilisateur courant.
    /// </summary>
    public sealed class CurrentUserSessionInfo
    {
        public string UserId { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public List<UserRoleEnum> Roles { get; set; } = [];
        public List<UserAreaEnum> AllowedAreas { get; set; } = [];
    }
}
