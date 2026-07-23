using BackPredictFinance.Common;
using BackPredictFinance.ViewModels.UserViewModels;
using Microsoft.EntityFrameworkCore;
using System.Net;

namespace BackPredictFinance.Services.AuthServices
{
    /// <summary>
    /// Gère les opérations RGPD de l'utilisateur courant : consentements, export de données
    /// et suppression (anonymisation) de compte.
    /// </summary>
    public interface IUserPrivacyService
    {
        Task<UserConsentsViewModel> GetConsentsAsync(CancellationToken ct = default);
        Task<UserConsentsViewModel> UpdateConsentsAsync(UpdateUserConsentsRequestViewModel request, CancellationToken ct = default);
        Task<DataExportResponseViewModel> RequestDataExportAsync(CancellationToken ct = default);
        Task DeleteCurrentAccountAsync(DeleteAccountRequestViewModel request, CancellationToken ct = default);
        Task<AlertPreferencesViewModel> GetAlertPreferencesAsync(CancellationToken ct = default);
        Task<AlertPreferencesViewModel> UpdateAlertPreferencesAsync(UpdateAlertPreferencesRequestViewModel request, CancellationToken ct = default);
    }

    /// <summary>
    /// Implémente les parcours RGPD adossés au compte de l'utilisateur courant.
    /// </summary>
    public sealed class UserPrivacyService : BaseService, IUserPrivacyService
    {
        public UserPrivacyService(IServiceProvider serviceProvider) : base(serviceProvider)
        {
        }

        public async Task<UserConsentsViewModel> GetConsentsAsync(CancellationToken ct = default)
        {
            var user = await GetCurrentUserAsync(ct);
            if (user is null)
            {
                throw new CustomException("Current user not found for GetConsents.", statusCode: HttpStatusCode.Unauthorized);
            }

            return new UserConsentsViewModel
            {
                AnalyticsConsent = user.AnalyticsConsent,
                MarketingEmailConsent = user.MarketingEmailConsent,
                ProductImprovementConsent = user.ProductImprovementConsent,
                LastUpdatedUtc = user.ConsentLastUpdatedUtc
            };
        }

        public async Task<UserConsentsViewModel> UpdateConsentsAsync(UpdateUserConsentsRequestViewModel request, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(request);

            var user = await GetCurrentUserAsync(ct);
            if (user is null)
            {
                throw new CustomException("Current user not found for UpdateConsents.", statusCode: HttpStatusCode.Unauthorized);
            }

            user.AnalyticsConsent = request.AnalyticsConsent;
            user.MarketingEmailConsent = request.MarketingEmailConsent;
            user.ProductImprovementConsent = request.ProductImprovementConsent;
            user.ConsentLastUpdatedUtc = DateTime.UtcNow;

            await _userManager.UpdateAsync(user);

            return new UserConsentsViewModel
            {
                AnalyticsConsent = user.AnalyticsConsent,
                MarketingEmailConsent = user.MarketingEmailConsent,
                ProductImprovementConsent = user.ProductImprovementConsent,
                LastUpdatedUtc = user.ConsentLastUpdatedUtc
            };
        }

        public Task<DataExportResponseViewModel> RequestDataExportAsync(CancellationToken ct = default)
        {
            _logger.LogInformation("[RGPD] Data export requested by user {UserId}", _currentUserId ?? "unknown");

            return Task.FromResult(new DataExportResponseViewModel
            {
                Status = "Pending",
                EstimatedDeliveryHours = 72,
                Message = "Votre demande d'export a été enregistrée. Vous recevrez un email dans les 72h."
            });
        }

        public async Task<AlertPreferencesViewModel> GetAlertPreferencesAsync(CancellationToken ct = default)
        {
            var user = await GetCurrentUserAsync(ct);
            if (user is null)
            {
                throw new CustomException("Current user not found for GetAlertPreferences.", statusCode: HttpStatusCode.Unauthorized);
            }

            return new AlertPreferencesViewModel
            {
                AlertPatternStateChangeEnabled = user.AlertPatternStateChangeEnabled,
                AlertLevelCrossedEnabled = user.AlertLevelCrossedEnabled,
                AlertDataStaleEnabled = user.AlertDataStaleEnabled
            };
        }

        public async Task<AlertPreferencesViewModel> UpdateAlertPreferencesAsync(UpdateAlertPreferencesRequestViewModel request, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(request);

            var user = await GetCurrentUserAsync(ct);
            if (user is null)
            {
                throw new CustomException("Current user not found for UpdateAlertPreferences.", statusCode: HttpStatusCode.Unauthorized);
            }

            user.AlertPatternStateChangeEnabled = request.AlertPatternStateChangeEnabled;
            user.AlertLevelCrossedEnabled = request.AlertLevelCrossedEnabled;
            user.AlertDataStaleEnabled = request.AlertDataStaleEnabled;

            await _userManager.UpdateAsync(user);

            return new AlertPreferencesViewModel
            {
                AlertPatternStateChangeEnabled = user.AlertPatternStateChangeEnabled,
                AlertLevelCrossedEnabled = user.AlertLevelCrossedEnabled,
                AlertDataStaleEnabled = user.AlertDataStaleEnabled
            };
        }

        public async Task DeleteCurrentAccountAsync(DeleteAccountRequestViewModel request, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(request);

            var user = await GetCurrentUserAsync(ct);
            if (user is null)
            {
                throw new CustomException("Current user not found for DeleteAccount.", statusCode: HttpStatusCode.Unauthorized);
            }

            var isPasswordValid = await _userManager.CheckPasswordAsync(user, request.CurrentPassword);
            if (!isPasswordValid)
            {
                throw new CustomException(
                    $"[RGPD] Delete account: invalid password for user {user.Id}",
                    "Mot de passe incorrect.",
                    statusCode: HttpStatusCode.BadRequest);
            }

            if (!request.ConfirmDeletion)
            {
                throw new CustomException(
                    $"[RGPD] Delete account: confirmation not provided for user {user.Id}",
                    "La confirmation de suppression est requise.",
                    statusCode: HttpStatusCode.BadRequest);
            }

            var userId = user.Id;
            user.Email = $"deleted_{userId}@deleted.local";
            user.UserName = $"deleted_{userId}@deleted.local";
            user.NormalizedEmail = $"DELETED_{userId.ToUpperInvariant()}@DELETED.LOCAL";
            user.NormalizedUserName = $"DELETED_{userId.ToUpperInvariant()}@DELETED.LOCAL";
            user.FirstName = "Compte";
            user.LastName = "Supprimé";
            user.PhoneNumber = null;
            user.IsActive = false;
            user.DeletedAt = DateTime.UtcNow;

            var refreshTokens = await _financeDbContext.RefreshTokens
                .Where(x => x.UserId == userId)
                .ToListAsync(ct);

            _financeDbContext.RefreshTokens.RemoveRange(refreshTokens);

            await _userManager.UpdateSecurityStampAsync(user);
            await _userManager.UpdateAsync(user);
            await _financeDbContext.SaveChangesAsync(ct);

            _logger.LogInformation("[RGPD] Account anonymized for user {UserId}", userId);
        }
    }
}
