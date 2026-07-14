using BackPredictFinance.Common.enums;
using BackPredictFinance.Datas.Entities;
using BackPredictFinance.Services;
using BackPredictFinance.ViewModels.UserViewModels;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace BackPredictFinance.Services.UserServices
{
    /// <summary>
    /// Fournit l'accès aux rôles utilisateurs et à leur projection frontend.
    /// </summary>
    public interface IUserRoleDataService
    {
        /// <summary>
        /// Synchronise les rôles persistés d'un utilisateur à partir d'une liste projetée.
        /// </summary>
        Task AddUserRole(List<UserRoleViewModel> rolesVm, User user);
        /// <summary>
        /// Retourne les rôles d'un utilisateur sous forme de view models.
        /// </summary>
        Task<List<UserRoleViewModel>> SetUserRoleViewModel(string userId);
        /// <summary>
        /// Vérifie si l'utilisateur ciblé possède un rôle donné.
        /// </summary>
        Task<bool> IsUserInRole(string userId, UserRoleEnum roleEnum);
        /// <summary>
        /// Vérifie si l'instance utilisateur fournie possède un rôle donné.
        /// </summary>
        Task<bool> IsUserInRole(User user, UserRoleEnum roleEnum);
        /// <summary>
        /// Retourne la liste des rôles connus sous forme projetée.
        /// </summary>
        Task<List<UserRoleViewModel>> GetAllRoles(CancellationToken ct);
        /// <summary>
        /// Retourne le premier utilisateur trouvé pour un rôle donné.
        /// </summary>
        Task<User?> GetFirstUserInRole(UserRoleEnum roleEnum);

    }

    /// <summary>
    /// Implémente la lecture et l'attribution des rôles utilisateurs.
    /// </summary>
    public class UserRoleDataService : BaseService, IUserRoleDataService
    {

        public UserRoleDataService(IServiceProvider serviceProvider)
            : base(serviceProvider)
        {
        }

        public async Task<User?> GetFirstUserInRole(UserRoleEnum roleEnum)
        {
            var roleName = roleEnum.ToString();

            var usersInRole = await _userManager.GetUsersInRoleAsync(roleName);

            return usersInRole.FirstOrDefault();
        }

        public async Task<List<UserRoleViewModel>> GetAllRoles(CancellationToken ct)
        {
            var listVm = new List<UserRoleViewModel>();
            var existingRoles = await _roleManager.Roles.ToListAsync(ct);

            foreach (var role in existingRoles)
            {
                if (!Enum.TryParse<UserRoleEnum>(role.Name, ignoreCase: true, out var userRole))
                {
                    continue;
                }

                var vm = new UserRoleViewModel
                {
                    RoleId = role.Id,
                    UserRole = userRole,
                };

                switch (userRole)
                {
                    case UserRoleEnum.Admin:
                        vm.RoleName = "Admin";
                        break;

                    case UserRoleEnum.User:
                        vm.RoleName = "User";
                        break;

                    default:
                        vm.RoleName = "Utilisateur";
                        break;
                }


                listVm.Add(vm);
            }

            return listVm;
        }

        public async Task AddUserRole(List<UserRoleViewModel> rolesVm, User user)
        {
            var newRoles = rolesVm.Select(r => r.UserRole.ToString()).ToList();

            var currentRoles = (await _userManager.GetRolesAsync(user)).ToList();

            // Ajoute les rles manquants
            foreach (var role in newRoles.Except(currentRoles))
            {
                await _userManager.AddToRoleAsync(user, role);
            }

            // Supprime les rles obsoltes
            foreach (var role in currentRoles.Except(newRoles))
            {
                await _userManager.RemoveFromRoleAsync(user, role);
            }
        }


        public async Task<bool> IsUserInRole(string userId, UserRoleEnum roleEnum)
        {
            var user = await _userManager.FindByIdAsync(userId);
            var roleName = roleEnum.ToString();

            return user != null && await _userManager.IsInRoleAsync(user, roleName);
        }

        public async Task<bool> IsUserInRole(User user, UserRoleEnum roleEnum)
        {
            var roleName = roleEnum.ToString();

            return user != null && await _userManager.IsInRoleAsync(user, roleName);
        }

        /// <summary>
        /// cr une liste de role pour un user. Le nom des roles est formats.
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        public async Task<List<UserRoleViewModel>> SetUserRoleViewModel(string userId)
        {
            var rawRoles = await _financeDbContext.UserRoles
                .Where(ur => ur.UserId == userId)
                .Join(_financeDbContext.Roles,
                      ur => ur.RoleId,
                      r => r.Id,
                      (ur, r) => new { r.Id, r.Name })
                .ToListAsync();

            // garde uniquement ou le parsing fonctionne
            var validRaw = rawRoles
                .Where(x => Enum.TryParse<UserRoleEnum>(x.Name, ignoreCase: true, out _));

            var vms = validRaw
                .Select(x =>
                {
                    var vm = new UserRoleViewModel
                    {
                        RoleId = x.Id
                    };

                    Enum.TryParse<UserRoleEnum>(x.Name, true, out var userRole);

                    vm.UserRole = userRole;
                    switch (userRole)
                    {
                        case UserRoleEnum.Admin:
                            vm.RoleName = "Admin";
                            break;

                        case UserRoleEnum.User:
                            vm.RoleName = "Consultant";
                            break;

                        default:
                            vm.RoleName = "nul";
                            break;
                    }

                    return vm;
                })
                .ToList();

            return vms;
        }




    }


}
