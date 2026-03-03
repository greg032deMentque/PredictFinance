using BackPredictFinance.Common;
using BackPredictFinance.Datas.Models;
using BackPredictFinance.ViewModels.CommonViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BackPredictFinance.Services
{
    public class AccountService : BaseService
    {
        private EmailService _emailService;
        private JwtGeneratorService _jwtGeneratorService;
        public AccountService(EmailService emailservice, IServiceProvider serviceProvider,  JwtGeneratorService jwtGeneratorService) : base(serviceProvider)
        {
            _emailService = emailservice;
            _jwtGeneratorService = jwtGeneratorService;
        }


    
        /// <summary>
        /// Login user
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public async Task<TokenViewModel?> Login(LoginViewModel model)
        {
            if (model.Password.Trim() == string.Empty)
                return null;

            var user = await _userManager.Users.FirstOrDefaultAsync(u => u.Email == model.Email);
            if (user == null)
                return null;

            if (await _userManager.IsLockedOutAsync(user))
            {
                _logger.LogWarning($"[BACKOFFICE] Account manually locked: {model.Email}");
                return null;
            }

            // IMPORTANT : ici, lockoutOnFailure = false => on ne laisse pas ASP.NET gérer l'échec
            var result = await _signInManager.CheckPasswordSignInAsync(user, model.Password, lockoutOnFailure: false);

            if (result.Succeeded)
            {

                user.LastConnection = DateTime.Now;
                var refreshResult = _jwtGeneratorService.GenerateRefreshToken();
                await _jwtGeneratorService.UpdateRefreshTokenAsync(user.Id, refreshResult);


                await _userManager.ResetAccessFailedCountAsync(user);
                await _userManager.UpdateAsync(user);

                var token = await _jwtGeneratorService.GenerateJwtToken(user);
                _logger.LogInformation($"[BACKOFFICE] {user.Id} connected.");

                await _financeDbContext.SaveChangesAsync();

                return new TokenViewModel
                {
                    Token = token,
                    Firstname = user.FirstName,
                    Lastname = user.LastName,
                    RefreshToken = user.RefreshToken
                };
            }

            _logger.LogWarning($"[BACKOFFICE] Failed login attempt for: {model.Email}");

            // Gérer le compteur manuellement
            await _userManager.AccessFailedAsync(user);

            var failedCount = await _userManager.GetAccessFailedCountAsync(user);

            if (failedCount >= 5)
            {
                await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue); // blocage définitif
                _logger.LogWarning($"[BACKOFFICE] {model.Email} account permanently locked.");
            }

            return null;
        }


        public async Task<TokenViewModel> LoginMobile(LoginViewModel model)
        {
            if (model.Password.Trim() == string.Empty)
                return null;

            var user = await _userManager.Users
             .FirstOrDefaultAsync(u => u.Email == model.Email);

            if (user == null)
                return null;


            // Vérifie si le compte est temporairement bloqué
            if (await _userManager.IsLockedOutAsync(user))
            {
                _logger.LogError($"[MOBILE] Account temporarily locked: {model.Email}");
                return null;
            }

            // Tente la connexion avec lockout activé
            var result = await _signInManager.PasswordSignInAsync(model.Email, model.Password, true, lockoutOnFailure: true);

            if (result.Succeeded)
            {


                user.LastConnection = DateTime.Now;

                await RegisterDevice(model.MobileId, user.Id);

                var refreshResult = _jwtGeneratorService.GenerateRefreshToken();
                await _jwtGeneratorService.UpdateRefreshTokenAsync(user.Id, refreshResult);


                await _userManager.ResetAccessFailedCountAsync(user);
                await _userManager.UpdateAsync(user);

                var token = await _jwtGeneratorService.GenerateJwtToken(user);
                _logger.LogInformation($"[MOBILE] {user.Id} connected.");

                return new TokenViewModel
                {
                    Token = token,
                    Firstname = user.FirstName,
                    Lastname = user.LastName,
                    RefreshToken = user.RefreshToken
                };
            }

            if (result.IsLockedOut)
            {
                _logger.LogWarning($"[MOBILE] User account locked out: {model.Email}");

                // passage en blocage permanent après 3 blocages temporaires
                var accessFailedCount = await _userManager.GetAccessFailedCountAsync(user);
                if (accessFailedCount >= 9)
                {
                    await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue);
                    _logger.LogWarning($"[MOBILE] {model.Email} account permanently locked.");
                }
            }
            else
            {
                _logger.LogWarning($"[MOBILE] Failed login attempt for: {model.Email}");
            }

            return null;
        }


        public async Task UnlockUser(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);

            if (user == null)
            {
                throw new CustomException($"User not found {userId}");
            }

            // Réinitialise le verrouillage
            await _userManager.SetLockoutEndDateAsync(user, null);
            await _userManager.ResetAccessFailedCountAsync(user);

            _logger.LogInformation($"[ADMIN] Compte débloqué pour l'utilisateur : {user.Email}");
        }

        public async Task ResetPassword(ResetPasswordPayloadViewModel model)
        {
            var user = await _userManager.FindByEmailAsync(model.Email);

            // par sécurité on ne renvoit pas une erreur. 
            if (user == null)
            {
                _logger.LogError($"user with this email not found : {model.Email}");
                return;
            }

            var decodedToken = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(model.Token));
            var result = await _userManager.ResetPasswordAsync(user, decodedToken, model.Password);
            if (!result.Succeeded)
            {
                _logger.LogError(string.Join(Environment.NewLine, result.Errors.Select(x => x.Description)));
            }

        }

        public async Task RegisterDevice(string mobileId, string userId)
        {
            var user = await _financeDbContext.Users
            .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null) throw new CustomException("User was null");
            if (string.IsNullOrEmpty(mobileId)) throw new CustomException("mobileId was null", "Une erreur est survenue pour récupérer l'identifiant de votre téléphone");

            await _financeDbContext.SaveChangesAsync();
        }



        /// <summary>
        /// Log out user
        /// </summary>
        /// <returns></returns>
        public async Task Logout()
        {
            await _signInManager.SignOutAsync();
        }

        public async Task ForgotPassword(string email)
        {
            var user = await _userManager.FindByEmailAsync(email);

            // par sécurité on ne renvoit pas une erreur.
            if (user == null)
            {
                _logger.LogError($"this mail does not exist {email}");
                return;
            }

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var link = $"{_configuration.GetSection("domain").Value}/ForgotPassword?token={WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token))}&email={user.Email}";

            var result = _emailService.SendEmailPasswordReset(user.Email, link);
        }


        public async Task ChangePassword(ChangePasswordViewModel resetPassword)
        {
            if (_currentUser == null)
                throw new CustomException($"An unknow user try to change a password");

            if (resetPassword.NewPassword != resetPassword.ConfirmNewPassword)
                throw new CustomException($"Password and confirm password does not match", "Le mot de passe et la confirmation du mot de passe ne sont pas identiques");

            var result = await _userManager.ChangePasswordAsync(_currentUser, resetPassword.CurrentPassword, resetPassword.NewPassword);
            if (!result.Succeeded)
            {
                var error = $"Problème pour réinitialiser le mot de passe {_currentUser.Email}. ";

                throw new CustomException(error + string.Join(Environment.NewLine, result.Errors.Select(x => x.Description)));
            }
        }


        public async Task<TokenViewModel> RefreshTokenAsync(TokenViewModel request)
        {
            var principal = _jwtGeneratorService.GetPrincipalFromExpiredToken(request.Token);
            if (principal == null)
            {
                _logger.LogError($"invalid access token");
                return null;
            }

            foreach (var claim in principal.Claims)
            {
                _logger.LogInformation($"Type: {claim.Type}, Value: {claim.Value}");
            }

            var userId = principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                ?? principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;


            var storedRefreshToken = await _jwtGeneratorService.GetRefreshTokenAsync(userId);
            if (storedRefreshToken == null || storedRefreshToken != request.RefreshToken)
            {
                _logger.LogError($"invalid refresh token");
                return null;
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                _logger.LogError($"user not found");
                return null;
            }

            var newAccessToken = await _jwtGeneratorService.GenerateJwtToken(user);
            var newRefreshToken = _jwtGeneratorService.GenerateRefreshToken();
            await _jwtGeneratorService.UpdateRefreshTokenAsync(userId, newRefreshToken);

            return new TokenViewModel
            {
                Token = newAccessToken,
                RefreshToken = newRefreshToken,
                Firstname = user.FirstName,
                Lastname = user.LastName,
            };
        }



    }

   
}
