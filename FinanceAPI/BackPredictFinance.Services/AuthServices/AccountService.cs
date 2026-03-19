using BackPredictFinance.Common;
using BackPredictFinance.Common.enums;
using BackPredictFinance.Datas.Entities;
using BackPredictFinance.ViewModels.UserViewModels.AuthViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using System.Security;
using System.Text;

namespace BackPredictFinance.Services.AuthServices
{
    public interface IAccountService
    {
        Task<TokenViewModel?> Login(LoginViewModel model);
        Task<TokenViewModel?> LoginAdmin(LoginViewModel model);
        Task<TokenViewModel?> RefreshTokenAsync(TokenViewModel request);
        Task UnlockUser(string userId);
        Task<IdentityResult> ResetPassword(ResetPasswordRequestViewModel model);
        Task RegisterDevice(string mobileId, string userId);
        Task Logout(TokenViewModel? request);
        Task ForgotPassword(string email);
        Task ChangePassword(ChangePasswordViewModel resetPassword);
    }

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

            if (requiresAdmin && !await IsAdminAsync(user))
            {
                _logger.LogWarning("[ADMIN_AUTH] Non-admin tried to login as admin: {Email}", model.Email);
                return null;
            }

            if (requiresAdmin)
            {
                _logger.LogInformation("[ADMIN_AUTH] Admin login success: {Email} ({Id})", user.Email, user.Id);
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
            _logger.LogInformation("[ADMIN] Compte debloque pour l'utilisateur : {Email}", user.Email);
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

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user is null)
            {
                return IdentityResult.Failed(new IdentityError { Description = "Utilisateur introuvable." });
            }

            var token = DecodeBase64UrlToUtf8(model.Token);
            return await _userManager.ResetPasswordAsync(user, token, model.Password);
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
            if (user is null || !await _userManager.IsEmailConfirmedAsync(user))
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
            return await _userManager.IsInRoleAsync(user, UserRoleEnum.Admin.ToString())
                || await _userManager.IsInRoleAsync(user, UserRoleEnum.SuperAdmin.ToString());
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
