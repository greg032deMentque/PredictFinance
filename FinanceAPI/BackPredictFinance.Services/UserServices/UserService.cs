using BackPredictFinance.Common;
using BackPredictFinance.Datas.Common;
using BackPredictFinance.Datas.Context;
using BackPredictFinance.Datas.Models;
using BackPredictFinance.ViewModels.CommonViewModels;
using BackPredictFinance.ViewModels.UserViewModels;
using BackPredictFinance.ViewModels.WebViewModels.PaginateViewModels;
using BackPredictFinance.ViewModels.WebViewModels.PaginateViewModels.Filters;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.JsonWebTokens;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace BackPredictFinance.Services.UserServices
{
    public interface IUserService
    {
        // Création d'un utilisateur
        Task<UserViewModel> CreateUser(UserViewModel user);
        // Récupération d'un utilisateur par ID
        Task<UserViewModel> GetUserById(string userId);
        // Récupération de tous les utilisateurs
        Task<List<UserViewModel>> GetAllUsers();
        // Mise à jour d'un utilisateur
        Task<UserViewModel> UpdateUser(UserViewModel user);
        // Suppression d'un utilisateur
        Task DeleteUser(string userId);

        // Génération et confirmation d'email
        Task<string> GenerateEmailConfirmationToken(UserViewModel user);
        Task<UserViewModel> ConfirmEmail(UserViewModel user, string token);

        // Génération et réinitialisation de mot de passe
        Task<string> GeneratePasswordResetToken(UserViewModel user);
        Task<UserViewModel> ResetPassword(UserViewModel user, string token, string newPassword);

        // Connexion et tokens JWT
        Task<TokenViewModel> Login(LoginViewModel loginModel);
        Task<TokenViewModel> RefreshTokenAsync(TokenViewModel tokenRequest);
        Task<string> RenewRefreshToken(UserViewModel user);
        Task Logout();

        // Mot de passe oublié et déverrouillage
        Task ForgotPassword(string email);
        Task UnlockUser(string userId);
        Task ChangePassword(ChangePasswordViewModel changePasswordModel);

        // Pagination utilisateur par companyId
        Task<UserPaginateViewModel> GetByPaginationByCompanyId(PaginateViewModel paginateVm, string companyId);
    }

    public class UserService : BaseService, IUserService
    {
        private readonly JwtGeneratorService _jwtGenerator;
        private readonly UserRoleDataService _userRoleDataService;
        private readonly EmailService _emailService;

        // Utilisation du service de logs centralisé
        public ILogService _logger;

        public UserService(
            IServiceProvider serviceProvider,
            JwtGeneratorService jwtGenerator,
            UserRoleDataService userRoleDataService,
            EmailService emailService) : base(serviceProvider)
        {
            _jwtGenerator = jwtGenerator;
            _userRoleDataService = userRoleDataService;
            _emailService = emailService;
            _logger = serviceProvider.GetRequiredService<ILogService>();
        }

        /// <summary>Crée un nouvel utilisateur</summary>
        public async Task<UserViewModel> CreateUser(UserViewModel vm)
        {
            vm.Id = Guid.NewGuid().ToString();
            var user = vm.ToEntity();
            user.CreatedAt = DateTime.UtcNow;

            _logger.LogInformation("Création user: {UserName}");
            foreach (var validator in _userManager.PasswordValidators)
            {
                var validation = await validator.ValidateAsync(_userManager, user, vm.Password);
                if (!validation.Succeeded)
                    throw new CustomException(string.Join("; ", validation.Errors.Select(e => e.Description)));
            }

            var result = await _userManager.CreateAsync(user, vm.Password);
            if (!result.Succeeded)
                throw new CustomException(string.Join("; ", result.Errors.Select(e => e.Description)));

            return user.ToViewModel();
        }

        /// <summary>Récupère un utilisateur par son ID</summary>
        public async Task<UserViewModel> GetUserById(string userId)
            => (await _userManager.FindByIdAsync(userId))?.ToViewModel();

        /// <summary>Récupère tous les utilisateurs</summary>
        public Task<List<UserViewModel>> GetAllUsers()
            => Task.FromResult(_userManager.Users.Select(u => u.ToViewModel()).ToList());

        /// <summary>Met à jour un utilisateur existant</summary>
        public async Task<UserViewModel> UpdateUser(UserViewModel vm)
        {
            var existing = await _userManager.FindByIdAsync(vm.Id);
            if (existing == null) return null;
            _logger.LogInformation("Mise à jour user: {UserName}");
            existing.UserName = vm.UserName;
            existing.Email = vm.Email;
            existing.PhoneNumber = vm.PhoneNumber;
            existing.FirstName = vm.FirstName;
            existing.LastName = vm.LastName;
            existing.UpdatedAt = DateTime.UtcNow;
            var res = await _userManager.UpdateAsync(existing);
            return res.Succeeded ? existing.ToViewModel() : null;
        }

        /// <summary>Supprime un utilisateur</summary>
        public async Task DeleteUser(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId) ?? throw new CustomException("User not found");
            _logger.LogInformation("Suppression user: {UserName}");
            await _userManager.DeleteAsync(user);
        }

       

        /// <summary>Génère un token de confirmation email</summary>
        public async Task<string> GenerateEmailConfirmationToken(UserViewModel vm)
            => await _userManager.GenerateEmailConfirmationTokenAsync(await _userManager.FindByIdAsync(vm.Id));

        /// <summary>Confirme l'email d'un utilisateur</summary>
        public async Task<UserViewModel> ConfirmEmail(UserViewModel vm, string token)
        {
            var user = await _userManager.FindByIdAsync(vm.Id);
            return (await _userManager.ConfirmEmailAsync(user, token)).Succeeded ? user.ToViewModel() : null;
        }

        /// <summary>Génère un token de reset mot de passe</summary>
        public async Task<string> GeneratePasswordResetToken(UserViewModel vm)
            => await _userManager.GeneratePasswordResetTokenAsync(await _userManager.FindByIdAsync(vm.Id));

        /// <summary>Réinitialise le mot de passe d'un utilisateur</summary>
        public async Task<UserViewModel> ResetPassword(UserViewModel vm, string token, string newPassword)
        {
            var user = await _userManager.FindByIdAsync(vm.Id);
            return (await _userManager.ResetPasswordAsync(user, token, newPassword)).Succeeded ? user.ToViewModel() : null;
        }

        /// <summary>Authentifie un utilisateur et retourne un JWT</summary>
        public async Task<TokenViewModel> Login(LoginViewModel loginModel)
        {
            var user = await _userManager.FindByEmailAsync(loginModel.Email) ?? throw new CustomException("Invalid credentials");
            var signIn = await _signInManager.CheckPasswordSignInAsync(user, loginModel.Password, false);
            if (!signIn.Succeeded) throw new CustomException("Invalid credentials");
            var jwt = await _jwtGenerator.GenerateJwtToken(user);
            var refresh = _jwtGenerator.GenerateRefreshToken();
            await _jwtGenerator.UpdateRefreshTokenAsync(user.Id, refresh);
            return new TokenViewModel { Token = jwt, RefreshToken = refresh, Firstname = user.FirstName, Lastname = user.LastName };
        }

        /// <summary>Rafraîchit un JWT via le refresh token</summary>
        public async Task<TokenViewModel> RefreshTokenAsync(TokenViewModel request)
        {
            var principal = _jwtGenerator.GetPrincipalFromExpiredToken(request.Token) ?? throw new CustomException("Invalid token");
            var userId = principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
            var stored = await _jwtGenerator.GetRefreshTokenAsync(userId);
            if (stored != request.RefreshToken) throw new CustomException("Invalid refresh token");
            var user = await _userManager.FindByIdAsync(userId);
            var newJwt = await _jwtGenerator.GenerateJwtToken(user);
            var newRefresh = _jwtGenerator.GenerateRefreshToken();
            await _jwtGenerator.UpdateRefreshTokenAsync(userId, newRefresh);
            return new TokenViewModel { Token = newJwt, RefreshToken = newRefresh, Firstname = user.FirstName, Lastname = user.LastName };
        }

        /// <summary>Renouvelle uniquement le refresh token</summary>
        public async Task<string> RenewRefreshToken(UserViewModel vm)
        {
            var user = await _userManager.FindByIdAsync(vm.Id) ?? throw new CustomException("User not found");
            var newRefresh = _jwtGenerator.GenerateRefreshToken();
            await _jwtGenerator.UpdateRefreshTokenAsync(user.Id, newRefresh);
            return newRefresh;
        }

        /// <summary>Déconnecte l'utilisateur</summary>
        public async Task Logout()
            => await _signInManager.SignOutAsync();

        /// <summary>Envoie un email de réinitialisation de mot de passe</summary>
        public async Task ForgotPassword(string email)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null) { _logger.LogWarning("Email non trouvé"); return; }
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var link = $"{_configuration["domain"]}/ForgotPassword?token={WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token))}&email={user.Email}";
            _emailService.SendEmailPasswordReset(user.Email, link);
        }

        /// <summary>Déverrouille un utilisateur verrouillé</summary>
        public async Task UnlockUser(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId) ?? throw new CustomException("User not found");
            await _userManager.SetLockoutEndDateAsync(user, null);
            await _userManager.ResetAccessFailedCountAsync(user);
        }

        /// <summary>Change le mot de passe de l'utilisateur courant</summary>
        public async Task ChangePassword(ChangePasswordViewModel model)
        {
            var current = await GetCurrentUserAsync() ?? throw new CustomException("Unauthorized");
            if (model.NewPassword != model.ConfirmNewPassword) throw new CustomException("Passwords do not match");
            var res = await _userManager.ChangePasswordAsync(current, model.CurrentPassword, model.NewPassword);
            if (!res.Succeeded) throw new CustomException(string.Join("; ", res.Errors.Select(e => e.Description)));
        }

        /// <summary>Pagination des utilisateurs par companyId</summary>
        public async Task<UserPaginateViewModel> GetByPaginationByCompanyId(PaginateViewModel paginateVm, string companyId)
        {
            var filter = string.IsNullOrWhiteSpace(paginateVm.Filter)
                ? new UserFilter()
                : JsonSerializer.Deserialize<UserFilter>(paginateVm.Filter);
            var predicate = GetFilter(companyId, filter);
            var users = await _financeDbContext.Set<User>().GetByPaginationAsync(
                paginateVm.PageIndex * paginateVm.PageSize,
                paginateVm.PageSize,
                paginateVm.SortActive,
                paginateVm.SortDirection,
                predicate);
            var vms = users.Select(u => u.ToViewModel()).ToList();
            var total = await _financeDbContext.Users.GetTotalCountAsync(predicate);
            var result = new UserPaginateViewModel(total, vms);
            foreach (var uvm in result.Datas)
                uvm.Roles = await _userRoleDataService.SetUserRoleViewModel(uvm.Id);
            return result;
        }

        private Expression<Func<User, bool>> GetFilter(string companyId, UserFilter filter)
            => u =>
                (string.IsNullOrWhiteSpace(filter.Name) ||
                 (u.FirstName + " " + u.LastName).ToLower().Contains(filter.Name.ToLower())) &&
                (!filter.ActiveStatus.HasValue || u.IsActive == filter.ActiveStatus.Value);
    }
}
