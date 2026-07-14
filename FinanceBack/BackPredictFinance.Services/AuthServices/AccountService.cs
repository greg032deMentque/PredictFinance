using BackPredictFinance.Common;
using BackPredictFinance.Common.Email;
using BackPredictFinance.Common.enums;
using BackPredictFinance.Datas.Entities;
using BackPredictFinance.ViewModels.UserViewModels;
using BackPredictFinance.ViewModels.UserViewModels.AuthViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Security;
using System.Text;

namespace BackPredictFinance.Services.AuthServices
{
    /// <summary>
    /// Gère l'authentification, la rotation des tokens et les opérations de compte.
    /// </summary>
    public interface IAccountService
    {
        /// <summary>
        /// Authentifie un utilisateur standard avec email et mot de passe.
        /// </summary>
        Task<TokenViewModel?> Login(LoginViewModel model);
        /// <summary>
        /// Authentifie un utilisateur sur le parcours administrateur.
        /// </summary>
        Task<TokenViewModel?> LoginAdmin(LoginViewModel model);
        /// <summary>
        /// Renouvelle un couple access token / refresh token à partir d'un refresh token présenté.
        /// </summary>
        Task<TokenViewModel?> RefreshTokenAsync(TokenViewModel request);
        /// <summary>
        /// Confirme l'email d'un compte à partir d'un jeton Identity.
        /// </summary>
        Task ConfirmEmailAsync(ConfirmEmailViewModel model);
        /// <summary>
        /// Réémet un email de confirmation si le compte existe et n'est pas encore confirmé.
        /// </summary>
        Task ResendConfirmationEmailAsync(string email);
        /// <summary>
        /// Envoie un email de confirmation pour un compte créé sur le parcours public.
        /// </summary>
        Task SendConfirmationEmailAsync(User user);
        /// <summary>
        /// Déverrouille un compte utilisateur précédemment verrouillé.
        /// </summary>
        Task UnlockUser(string userId);
        /// <summary>
        /// Réinitialise le mot de passe d'un utilisateur à partir d'un jeton de reset.
        /// </summary>
        Task<IdentityResult> ResetPassword(ResetPasswordRequestViewModel model);
        /// <summary>
        /// Enregistre un identifiant de terminal associé à un utilisateur.
        /// </summary>
        Task RegisterDevice(string mobileId, string userId);
        /// <summary>
        /// Termine la session courante et révoque le refresh token fourni si présent.
        /// </summary>
        Task Logout(TokenViewModel? request);
        /// <summary>
        /// Déclenche l'envoi d'un lien de réinitialisation de mot de passe.
        /// </summary>
        Task ForgotPassword(string email);
        /// <summary>
        /// Modifie le mot de passe de l'utilisateur authentifié.
        /// </summary>
        Task ChangePassword(ChangePasswordViewModel resetPassword);
    }

    /// <summary>
    /// Implémente les parcours de connexion, déconnexion et gestion de mot de passe.
    /// </summary>
    public sealed class AccountService : BaseService, IAccountService
    {
        private readonly IEmailService _emailService;
        private readonly IJwtGeneratorService _jwtGeneratorService;

        public AccountService(
            IEmailService emailService,
            IServiceProvider serviceProvider,
            IJwtGeneratorService jwtGeneratorService)
            : base(serviceProvider)
        {
            _emailService = emailService;
            _jwtGeneratorService = jwtGeneratorService;
        }

        public Task<TokenViewModel?> Login(LoginViewModel model)
        {
            return LoginWithPasswordInternal(model, requiresAdmin: false);
        }

        public Task<TokenViewModel?> LoginAdmin(LoginViewModel model)
        {
            return LoginWithPasswordInternal(model, requiresAdmin: true);
        }

        private async Task<TokenViewModel?> LoginWithPasswordInternal(LoginViewModel model, bool requiresAdmin)
        {
            if (string.IsNullOrWhiteSpace(model?.Email) || string.IsNullOrWhiteSpace(model.Password))
            {
                return null;
            }

            var user = await _userManager.Users.FirstOrDefaultAsync(u => u.Email == model.Email);
            if (user is null)
            {
                return null;
            }

            if (await _userManager.IsLockedOutAsync(user))
            {
                LogLocked(requiresAdmin, model.Email);
                return null;
            }

            var isPasswordValid = await ValidatePasswordAndLockoutAsync(user, model.Email, model.Password, requiresAdmin);
            if (!isPasswordValid)
            {
                return null;
            }

            if (!user.EmailConfirmed)
            {
                throw new CustomException(
                    $"[AUTH] Unconfirmed account: {model.Email}",
                    "Confirmez votre adresse email avant de vous connecter.",
                    statusCode: HttpStatusCode.Forbidden);
            }

            if (!user.IsActive)
            {
                LogInactive(requiresAdmin, model.Email);
                throw new CustomException(
                    $"[AUTH] Inactive account: {model.Email}",
                    "Votre accès est indisponible.",
                    statusCode: HttpStatusCode.Forbidden);
            }

            if (requiresAdmin && !await IsAdminAsync(user))
            {
                _logger.LogWarning("[ADMIN_AUTH] Non-admin tried to login as admin: {Email}", model.Email);
                return null;
            }

            if (requiresAdmin)
            {
                _logger.LogInformation("[ADMIN_AUTH] Admin login success: {Email} ({Id})", user.Email ?? string.Empty, user.Id);
            }

            return await IssueTokensAsync(user, mobileId: null);
        }

        private async Task<TokenViewModel> IssueTokensAsync(User user, string? mobileId)
        {
            if (await _userManager.IsLockedOutAsync(user))
            {
                throw new CustomException("Compte verrouille.");
            }

            await _userManager.ResetAccessFailedCountAsync(user);

            var accessToken = await _jwtGeneratorService.GenerateJwtToken(user);
            var refresh = await _jwtGeneratorService.GenerateUserRefreshToken(user, mobileId);

            return new TokenViewModel
            {
                Token = accessToken,
                RefreshToken = refresh.Token,
                RefreshTokenExpiresAtUtc = refresh.ExpiresUtc,
                FirstName = user.FirstName,
                LastName = user.LastName,
                IsFirstConnection = false
            };
        }

        private async Task<bool> ValidatePasswordAndLockoutAsync(User user, string email, string password, bool requiresAdmin)
        {
            var result = await _signInManager.CheckPasswordSignInAsync(user, password, lockoutOnFailure: false);
            if (result.Succeeded)
            {
                await _userManager.ResetAccessFailedCountAsync(user);
                return true;
            }

            LogFailed(requiresAdmin, email);
            await _userManager.AccessFailedAsync(user);

            var failedCount = await _userManager.GetAccessFailedCountAsync(user);
            if (failedCount >= 5)
            {
                await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue);
                LogLockedPermanent(requiresAdmin, email);
            }

            return false;
        }

        private static string DecodeBase64UrlToUtf8(string token)
        {
            var decodedBytes = WebEncoders.Base64UrlDecode(token);
            return Encoding.UTF8.GetString(decodedBytes);
        }

        public async Task UnlockUser(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                throw new ArgumentException("userId est requis.", nameof(userId));
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user is null)
            {
                throw new CustomException($"User not found {userId}");
            }

            await _userManager.SetLockoutEndDateAsync(user, null);
            await _userManager.ResetAccessFailedCountAsync(user);
            _logger.LogInformation("[ADMIN] Compte debloque pour l'utilisateur : {Email}", user.Email ?? string.Empty);
        }

        public async Task<IdentityResult> ResetPassword(ResetPasswordRequestViewModel model)
        {
            if (model is null)
            {
                throw new ArgumentNullException(nameof(model));
            }

            if (string.IsNullOrWhiteSpace(model.Email)
                || string.IsNullOrWhiteSpace(model.Token)
                || string.IsNullOrWhiteSpace(model.Password))
            {
                return IdentityResult.Failed(new IdentityError { Description = "Donnees invalides." });
            }

            if (!string.Equals(model.Password, model.ConfirmPassword, StringComparison.Ordinal))
            {
                return IdentityResult.Failed(new IdentityError { Description = "Le mot de passe et la confirmation ne correspondent pas." });
            }

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user is null)
            {
                return IdentityResult.Failed(new IdentityError { Description = "Utilisateur introuvable." });
            }

            var token = DecodeBase64UrlToUtf8(model.Token);
            return await _userManager.ResetPasswordAsync(user, token, model.Password);
        }

        public async Task ConfirmEmailAsync(ConfirmEmailViewModel model)
        {
            ArgumentNullException.ThrowIfNull(model);

            if (string.IsNullOrWhiteSpace(model.Email) || string.IsNullOrWhiteSpace(model.Token))
            {
                throw new CustomException(
                    "Email confirmation payload is incomplete.",
                    "Lien invalide ou expiré.",
                    statusCode: HttpStatusCode.BadRequest);
            }

            var user = await _userManager.FindByEmailAsync(model.Email.Trim());
            if (user is null)
            {
                throw new CustomException(
                    $"Email confirmation user not found: {model.Email}",
                    "Lien invalide ou expiré.",
                    statusCode: HttpStatusCode.BadRequest);
            }

            var normalizedToken = Uri.UnescapeDataString(model.Token.Trim());
            var result = await _userManager.ConfirmEmailAsync(user, normalizedToken);
            if (!result.Succeeded)
            {
                throw new CustomException(
                    $"Email confirmation failed for {model.Email}. {string.Join(" | ", result.Errors.Select(x => x.Description))}",
                    "Lien invalide ou expiré.",
                    statusCode: HttpStatusCode.BadRequest);
            }
        }

        public async Task ResendConfirmationEmailAsync(string email)
        {
            var normalizedEmail = email?.Trim();
            if (string.IsNullOrWhiteSpace(normalizedEmail))
            {
                return;
            }

            var user = await _userManager.FindByEmailAsync(normalizedEmail);
            if (user is null || user.EmailConfirmed)
            {
                await Task.Delay(Random.Shared.Next(100, 300));
                return;
            }

            await SendConfirmationEmailAsync(user);
        }

        public async Task SendConfirmationEmailAsync(User user)
        {
            ArgumentNullException.ThrowIfNull(user);

            if (string.IsNullOrWhiteSpace(user.Email))
            {
                throw new CustomException("Cannot send confirmation email without a user email.");
            }

            var frontendUrl = _configuration["Frontend:BaseUrl"];
            if (string.IsNullOrWhiteSpace(frontendUrl))
            {
                frontendUrl = "http://localhost:4200";
            }

            var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            var encodedToken = Uri.EscapeDataString(token);
            var link = $"{frontendUrl.TrimEnd('/')}/register?email={Uri.EscapeDataString(user.Email)}&token={encodedToken}";
            var body = AccountEmailTemplates.BuildEmailConfirmationHtml(link);

            await _emailService.SendEmail(
                user.Email,
                "Confirmez votre adresse email - PredictFinance",
                body,
                isBodyHtml: true,
                attachments: null);
        }

        public Task RegisterDevice(string mobileId, string userId)
        {
            if (string.IsNullOrWhiteSpace(mobileId) || string.IsNullOrWhiteSpace(userId))
            {
                return Task.CompletedTask;
            }

            _logger.LogInformation("register device requested for user {UserId}", userId);
            return Task.CompletedTask;
        }

        public async Task Logout(TokenViewModel? request)
        {
            if (request is not null && !string.IsNullOrWhiteSpace(request.RefreshToken))
            {
                try
                {
                    await _jwtGeneratorService.RevokeRefreshAsync(request.RefreshToken);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning($"logout: refresh token revocation failed - {ex.Message}");
                }
            }

            await _signInManager.SignOutAsync();
        }

        public async Task ForgotPassword(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return;
            }

            var user = await _userManager.FindByEmailAsync(email);
            if (user is null)
            {
                await Task.Delay(Random.Shared.Next(100, 300));
                return;
            }

            var frontendUrl = _configuration["Frontend:BaseUrl"];
            if (string.IsNullOrWhiteSpace(frontendUrl))
            {
                frontendUrl = "http://localhost:4200";
            }

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
            var link = $"{frontendUrl}/reset-password?token={encodedToken}&email={Uri.EscapeDataString(user.Email!)}";

            await _emailService.SendEmailPasswordReset(user.Email!, link);
        }

        public async Task ChangePassword(ChangePasswordViewModel resetPassword)
        {
            if (resetPassword is null)
            {
                throw new ArgumentNullException(nameof(resetPassword));
            }

            var currentUser = await GetCurrentUserAsync();
            if (currentUser is null)
            {
                throw new CustomException("Unknown user tried to change a password");
            }

            if (!string.Equals(resetPassword.NewPassword, resetPassword.ConfirmNewPassword, StringComparison.Ordinal))
            {
                throw new CustomException(
                    "Password and confirm password does not match",
                    "Le mot de passe et la confirmation ne correspondent pas");
            }

            var result = await _userManager.ChangePasswordAsync(currentUser, resetPassword.CurrentPassword, resetPassword.NewPassword);
            if (!result.Succeeded)
            {
                throw new CustomException(
                    $"Erreur lors du changement de mot de passe pour {currentUser.Email}. {string.Join(" | ", result.Errors.Select(x => x.Description))}");
            }
        }

        public async Task<TokenViewModel?> RefreshTokenAsync(TokenViewModel request)
        {
            if (request is null || string.IsNullOrWhiteSpace(request.RefreshToken))
            {
                _logger.LogWarning("refresh: missing refresh token");
                return null;
            }

            try
            {
                var (newAccess, newRefresh) = await _jwtGeneratorService.RotateRefresh(request.RefreshToken, deviceId: null);
                return new TokenViewModel
                {
                    Token = newAccess,
                    RefreshToken = newRefresh.Token,
                    RefreshTokenExpiresAtUtc = newRefresh.ExpiresUtc,
                };
            }
            catch (SecurityException ex)
            {
                _logger.LogWarning(ex.Message);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "refresh: unexpected error");
                return null;
            }
        }

        private async Task<bool> IsAdminAsync(User user)
        {
            return await _userManager.IsInRoleAsync(user, UserRoleEnum.Admin.ToString());
        }

        private void LogLocked(bool requiresAdmin, string email)
        {
            if (requiresAdmin)
            {
                _logger.LogWarning("[ADMIN_AUTH] Locked account: {Email}", email);
                return;
            }

            _logger.LogWarning("[BACKOFFICE] Account locked: {Email}", email);
        }

        private void LogFailed(bool requiresAdmin, string email)
        {
            if (requiresAdmin)
            {
                _logger.LogWarning("[ADMIN_AUTH] Failed login attempt: {Email}", email);
                return;
            }

            _logger.LogWarning("[BACKOFFICE] Failed login attempt for: {Email}", email);
        }

        private void LogInactive(bool requiresAdmin, string email)
        {
            if (requiresAdmin)
            {
                _logger.LogWarning("[ADMIN_AUTH] Inactive account: {Email}", email);
                return;
            }

            _logger.LogWarning("[BACKOFFICE] Inactive account: {Email}", email);
        }

        private void LogLockedPermanent(bool requiresAdmin, string email)
        {
            if (requiresAdmin)
            {
                _logger.LogWarning("[ADMIN_AUTH] {Email} permanently locked.", email);
                return;
            }

            _logger.LogWarning("[BACKOFFICE] {Email} account permanently locked.", email);
        }
    }
}
