using BackPredictFinance.Common;
using BackPredictFinance.Common.Common;
using BackPredictFinance.Common.enums;
using BackPredictFinance.Datas.Common;
using BackPredictFinance.Datas.Entities;
using BackPredictFinance.Services.AuthServices;
using BackPredictFinance.ViewModels.UserViewModels;
using BackPredictFinance.ViewModels.UserViewModels.AuthViewModels;
using BackPredictFinance.ViewModels.WebViewModels.PaginateViewModels;
using Microsoft.EntityFrameworkCore;

namespace BackPredictFinance.Services.UserServices
{
    public interface IUserService
    {
        Task<TokenViewModel?> Register(UserViewModel model, CancellationToken ct = default);
        Task<UserViewModel> UpdateUser(UserViewModel model, CancellationToken ct = default);
        Task<UserViewModel> GetUserData(CancellationToken ct = default);
        Task DeleteUser(string userId, CancellationToken ct = default);
        Task<PagedResultViewModel<UserViewModel>> GetUsersPaged(PaginateSettingsViewModel model, CancellationToken ct = default);
        Task<UserViewModel> GetUserDetails(string userId, CancellationToken ct = default);
        Task<UserViewModel> CreateUserAdmin(AdminUserUpsertViewModel model, CancellationToken ct = default);
        Task<UserViewModel> UpdateUserAdmin(string userId, AdminUserUpsertViewModel model, CancellationToken ct = default);
    }

    public class UserService : BaseService, IUserService
    {
        private readonly IAccountService _accountService;
        private readonly IUserRoleDataService _userRoleDataService;

        public UserService(
            IServiceProvider serviceProvider,
            IUserRoleDataService userRoleDataService,
            IAccountService accountService)
            : base(serviceProvider)
        {
            _userRoleDataService = userRoleDataService;
            _accountService = accountService;
        }

        public async Task<PagedResultViewModel<UserViewModel>> GetUsersPaged(PaginateSettingsViewModel model, CancellationToken ct = default)
        {
            var sort = string.IsNullOrWhiteSpace(model.SortActive) ? "Email" : model.SortActive;
            var take = model.PageSize > 0 ? model.PageSize : 25;
            var start = model.PageIndex >= 0 ? model.PageIndex * take : 0;

            var query = _financeDbContext.Set<User>().AsNoTracking();

            if (!string.IsNullOrWhiteSpace(model.Filter))
            {
                var filter = model.Filter.Trim();
                query = query.Where(x =>
                    (x.FirstName != null && x.FirstName.Contains(filter)) ||
                    (x.LastName != null && x.LastName.Contains(filter)) ||
                    (x.Email != null && x.Email.Contains(filter)) ||
                    (x.PhoneNumber != null && x.PhoneNumber.Contains(filter)));
            }

            var total = await query.CountAsync(ct);
            var users = await query
                .OrderByDynamic(sort, model.SortDirection)
                .Skip(start)
                .Take(take)
                .ToListAsync(ct);

            var vmList = _mapper.Map<List<UserViewModel>>(users);
            foreach (var userViewModel in vmList.Where(x => !string.IsNullOrWhiteSpace(x.Id)))
            {
                userViewModel.Roles = await _userRoleDataService.SetUserRoleViewModel(userViewModel.Id!);
            }

            return new PagedResultViewModel<UserViewModel>
            {
                Items = vmList,
                Total = total,
                Page = model.PageIndex + 1,
                PageSize = take
            };
        }

        public async Task DeleteUser(string userId, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                throw new CustomException("userId is required");
            }

            if (string.Equals(_currentUserId, userId, StringComparison.Ordinal))
            {
                throw new CustomException("Un administrateur ne peut pas se supprimer lui-meme.");
            }

            var user = await _userManager.Users.FirstOrDefaultAsync(x => x.Id == userId, ct);
            if (user is null)
            {
                return;
            }

            var result = await _userManager.DeleteAsync(user);
            if (!result.Succeeded)
            {
                throw new InvalidOperationException(string.Join(" | ", result.Errors.Select(e => e.Description)));
            }

            await _financeDbContext.SaveChangesAsync(ct);
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

        public async Task<TokenViewModel?> Register(UserViewModel model, CancellationToken ct = default)
        {
            var existing = await _userManager.FindByEmailAsync(model.Email);
            if (existing != null)
            {
                return null;
            }

            var user = new User
            {
                UserName = model.Email,
                Email = model.Email,
                FirstName = model.FirstName,
                LastName = model.LastName,
                RefreshToken = string.Empty
            };

            var create = await _userManager.CreateAsync(user, model.Password ?? string.Empty);
            if (!create.Succeeded)
            {
                return null;
            }

            await _userManager.AddToRoleAsync(user, UserRoleEnum.User.ToString());

            var loginVm = new LoginViewModel
            {
                Email = model.Email,
                Password = model.Password ?? string.Empty
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

        public async Task<UserViewModel> GetUserDetails(string userId, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                throw new ArgumentException("userId is required", nameof(userId));
            }

            var user = await _userManager.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == userId, ct);

            if (user is null)
            {
                throw new KeyNotFoundException($"User {userId} not found");
            }

            var viewModel = _mapper.Map<UserViewModel>(user);
            viewModel.Roles = await _userRoleDataService.SetUserRoleViewModel(user.Id);
            return viewModel;
        }

        public async Task<UserViewModel> CreateUserAdmin(AdminUserUpsertViewModel model, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(model.Email))
            {
                throw new CustomException("Email is required.");
            }

            if (string.IsNullOrWhiteSpace(model.Password))
            {
                throw new CustomException("Password is required.");
            }

            var existing = await _userManager.FindByEmailAsync(model.Email);
            if (existing is not null)
            {
                throw new CustomException("A user with this email already exists.");
            }

            var user = new User
            {
                UserName = model.Email,
                Email = model.Email,
                FirstName = model.FirstName?.Trim() ?? string.Empty,
                LastName = model.LastName?.Trim() ?? string.Empty,
                IsActive = model.IsActive,
                RefreshToken = string.Empty
            };

            var createResult = await _userManager.CreateAsync(user, model.Password);
            if (!createResult.Succeeded)
            {
                throw new CustomException(string.Join(" | ", createResult.Errors.Select(x => x.Description)));
            }

            var targetRole = ResolveRoleName(model.Role);
            if (!await _roleManager.RoleExistsAsync(targetRole))
            {
                targetRole = UserRoleEnum.User.ToString();
            }

            await _userManager.AddToRoleAsync(user, targetRole);
            return await GetUserDetails(user.Id, ct);
        }

        public async Task<UserViewModel> UpdateUserAdmin(string userId, AdminUserUpsertViewModel model, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                throw new ArgumentException("userId is required", nameof(userId));
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user is null)
            {
                throw new KeyNotFoundException($"User {userId} not found");
            }

            var email = model.Email?.Trim();
            if (!string.IsNullOrWhiteSpace(email) && !string.Equals(user.Email, email, StringComparison.OrdinalIgnoreCase))
            {
                var existing = await _userManager.FindByEmailAsync(email);
                if (existing is not null && existing.Id != userId)
                {
                    throw new CustomException("Another user already uses this email.");
                }

                var setEmailResult = await _userManager.SetEmailAsync(user, email);
                if (!setEmailResult.Succeeded)
                {
                    throw new CustomException(string.Join(" | ", setEmailResult.Errors.Select(x => x.Description)));
                }

                var setUserNameResult = await _userManager.SetUserNameAsync(user, email);
                if (!setUserNameResult.Succeeded)
                {
                    throw new CustomException(string.Join(" | ", setUserNameResult.Errors.Select(x => x.Description)));
                }
            }

            user.FirstName = model.FirstName?.Trim() ?? user.FirstName;
            user.LastName = model.LastName?.Trim() ?? user.LastName;
            user.PhoneNumber = model.PhoneNumber?.Trim() ?? user.PhoneNumber;
            user.IsActive = model.IsActive;
            user.UpdatedAt = DateTime.UtcNow;

            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                throw new CustomException(string.Join(" | ", updateResult.Errors.Select(x => x.Description)));
            }

            var targetRole = ResolveRoleName(model.Role);
            if (await _roleManager.RoleExistsAsync(targetRole))
            {
                var currentRoles = await _userManager.GetRolesAsync(user);
                if (!currentRoles.Contains(targetRole, StringComparer.OrdinalIgnoreCase))
                {
                    if (currentRoles.Count > 0)
                    {
                        await _userManager.RemoveFromRolesAsync(user, currentRoles);
                    }
                    await _userManager.AddToRoleAsync(user, targetRole);
                }
            }

            return await GetUserDetails(userId, ct);
        }

        private static string ResolveRoleName(string? roleFromClient)
        {
            if (string.IsNullOrWhiteSpace(roleFromClient))
            {
                return UserRoleEnum.User.ToString();
            }

            if (Enum.TryParse<UserRoleEnum>(roleFromClient, ignoreCase: true, out var parsed))
            {
                return parsed.ToString();
            }

            return UserRoleEnum.User.ToString();
        }

       
    }
}



