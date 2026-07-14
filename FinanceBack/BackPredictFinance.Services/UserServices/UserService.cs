using BackPredictFinance.Common;
using BackPredictFinance.Common.enums;
using BackPredictFinance.Datas.Entities;
using BackPredictFinance.Services.AuthServices;
using BackPredictFinance.ViewModels.UserViewModels;
using BackPredictFinance.ViewModels.UserViewModels.AuthViewModels;
using Microsoft.EntityFrameworkCore;
using System.Net;

namespace BackPredictFinance.Services.UserServices
{
    /// <summary>
    /// Gère le cycle de vie self-service des comptes : inscription, profil courant et projection.
    /// L'administration des comptes est portée par <see cref="IUserAdminService"/>.
    /// </summary>
    public interface IUserService
    {
        /// <summary>
        /// Inscrit un utilisateur public et retourne son état de connexion initial.
        /// </summary>
        Task<PublicSignupResponseViewModel> RegisterPublic(PublicSignupRequestViewModel model, CancellationToken ct = default);
        /// <summary>
        /// Crée un utilisateur standard puis retourne son jeton de connexion.
        /// </summary>
        Task<TokenViewModel?> Register(UserViewModel model, CancellationToken ct = default);
        /// <summary>
        /// Met à jour le profil de l'utilisateur courant.
        /// </summary>
        Task<UserViewModel> UpdateUser(UserViewModel model, CancellationToken ct = default);
        /// <summary>
        /// Retourne les données projetées de l'utilisateur courant.
        /// </summary>
        Task<UserViewModel> GetUserData(CancellationToken ct = default);
        /// <summary>
        /// Retourne la projection de profil du compte courant.
        /// </summary>
        Task<CurrentUserProfileViewModel> GetCurrentUserProfile(CancellationToken ct = default);
        /// <summary>
        /// Met à jour la projection de profil du compte courant.
        /// </summary>
        Task<CurrentUserProfileViewModel> UpdateCurrentUserProfile(UpdateCurrentUserProfileRequestViewModel model, CancellationToken ct = default);
    }

    /// <summary>
    /// Implémente les cas d'usage self-service de gestion du compte courant.
    /// </summary>
    public class UserService : BaseService, IUserService
    {
        private readonly IAccountService _accountService;

        public UserService(
            IServiceProvider serviceProvider,
            IAccountService accountService)
            : base(serviceProvider)
        {
            _accountService = accountService;
        }

        public async Task<PublicSignupResponseViewModel> RegisterPublic(PublicSignupRequestViewModel model, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(model);

            var email = NormalizeRequiredEmail(model.Email);
            var existingUser = await _userManager.FindByEmailAsync(email);
            if (existingUser is not null)
            {
                await _accountService.ResendConfirmationEmailAsync(email);

                return new PublicSignupResponseViewModel
                {
                    Email = email,
                    IsActive = existingUser.IsActive,
                    CanLogin = false,
                    RequiresEmailConfirmation = true
                };
            }

            var user = await CreateUserAsync(email, model.Password, string.Empty, string.Empty, isActive: true, emailConfirmed: false);
            await _accountService.SendConfirmationEmailAsync(user);

            return new PublicSignupResponseViewModel
            {
                Email = user.Email ?? email,
                IsActive = user.IsActive,
                CanLogin = false,
                RequiresEmailConfirmation = true
            };
        }

        public async Task<UserViewModel> GetUserData(CancellationToken ct = default)
        {
            var user = await _financeDbContext.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == _currentUserId, ct);

            if (user is null)
            {
                throw new CustomException("User not found");
            }

            return _mapper.Map<UserViewModel>(user);
        }

        public async Task<CurrentUserProfileViewModel> GetCurrentUserProfile(CancellationToken ct = default)
        {
            var user = await _financeDbContext.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == _currentUserId, ct);

            if (user is null)
            {
                throw new CustomException("User not found");
            }

            return new CurrentUserProfileViewModel
            {
                Email = user.Email ?? string.Empty,
                FirstName = user.FirstName ?? string.Empty,
                LastName = user.LastName ?? string.Empty,
                PhoneNumber = user.PhoneNumber ?? string.Empty
            };
        }

        public async Task<CurrentUserProfileViewModel> UpdateCurrentUserProfile(UpdateCurrentUserProfileRequestViewModel model, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(model);

            var principal = _httpContextAccessor?.HttpContext?.User;
            if (principal is null)
            {
                throw new UnauthorizedAccessException();
            }

            var user = await _userManager.GetUserAsync(principal);
            if (user is null)
            {
                throw new UnauthorizedAccessException();
            }

            user.FirstName = model.FirstName?.Trim() ?? string.Empty;
            user.LastName = model.LastName?.Trim() ?? string.Empty;
            user.PhoneNumber = model.PhoneNumber?.Trim() ?? string.Empty;

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                throw new InvalidOperationException(string.Join(" | ", result.Errors.Select(e => e.Description)));
            }

            return new CurrentUserProfileViewModel
            {
                Email = user.Email ?? string.Empty,
                FirstName = user.FirstName ?? string.Empty,
                LastName = user.LastName ?? string.Empty,
                PhoneNumber = user.PhoneNumber ?? string.Empty
            };
        }

        public async Task<TokenViewModel?> Register(UserViewModel model, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(model);

            var email = NormalizeRequiredEmail(model.Email);
            var password = model.Password ?? string.Empty;

            await CreateUserAsync(
                email,
                password,
                model.FirstName?.Trim() ?? string.Empty,
                model.LastName?.Trim() ?? string.Empty,
                model.IsActive);

            var loginVm = new LoginViewModel
            {
                Email = email,
                Password = password
            };

            return await _accountService.Login(loginVm);
        }

        public async Task<UserViewModel> UpdateUser(UserViewModel model, CancellationToken ct = default)
        {
            var principal = _httpContextAccessor?.HttpContext?.User;
            if (principal is null)
            {
                throw new UnauthorizedAccessException();
            }

            var user = await _userManager.GetUserAsync(principal);
            if (user is null)
            {
                throw new UnauthorizedAccessException();
            }

            if (!string.IsNullOrWhiteSpace(model.FirstName))
            {
                user.FirstName = model.FirstName;
            }
            if (!string.IsNullOrWhiteSpace(model.LastName))
            {
                user.LastName = model.LastName;
            }
            if (!string.IsNullOrWhiteSpace(model.PhoneNumber))
            {
                user.PhoneNumber = model.PhoneNumber;
            }
            user.IsActive = model.IsActive;
            var result = await _userManager.UpdateAsync(user);

            if (!result.Succeeded)
            {
                throw new InvalidOperationException(string.Join(" | ", result.Errors.Select(e => e.Description)));
            }

            return _mapper.Map<UserViewModel>(user);
        }

        private async Task<User> CreateUserAsync(string email, string password, string firstName, string lastName, bool isActive, bool emailConfirmed = false)
        {
            if (string.IsNullOrWhiteSpace(password))
            {
                throw new CustomException(
                    "Password is required.",
                    "Le mot de passe est requis.",
                    statusCode: HttpStatusCode.BadRequest);
            }

            var existing = await _userManager.FindByEmailAsync(email);
            if (existing is not null)
            {
                throw new CustomException(
                    "A user with this email already exists.",
                    "Un compte existe deja pour cet email.",
                    statusCode: HttpStatusCode.Conflict);
            }

            var user = new User
            {
                UserName = email,
                Email = email,
                FirstName = firstName,
                LastName = lastName,
                IsActive = isActive,
                EmailConfirmed = emailConfirmed,
                RefreshToken = string.Empty
            };

            var createResult = await _userManager.CreateAsync(user, password);
            if (!createResult.Succeeded)
            {
                throw new CustomException(
                    "User creation failed.",
                    "Les donnees d'inscription sont invalides.",
                    errors: createResult.Errors.Select(x => x.Description).ToList(),
                    statusCode: (HttpStatusCode)422);
            }

            var addRoleResult = await _userManager.AddToRoleAsync(user, UserRoleEnum.User.ToString());
            if (!addRoleResult.Succeeded)
            {
                await _userManager.DeleteAsync(user);

                throw new CustomException(
                    "User role assignment failed.",
                    "Impossible de finaliser l'inscription.",
                    errors: addRoleResult.Errors.Select(x => x.Description).ToList(),
                    statusCode: HttpStatusCode.InternalServerError);
            }

            return user;
        }

        private static string NormalizeRequiredEmail(string? email)
        {
            var normalizedEmail = email?.Trim();
            if (string.IsNullOrWhiteSpace(normalizedEmail))
            {
                throw new CustomException(
                    "Email is required.",
                    "L'email est requis.",
                    statusCode: HttpStatusCode.BadRequest);
            }

            return normalizedEmail;
        }
    }
}
