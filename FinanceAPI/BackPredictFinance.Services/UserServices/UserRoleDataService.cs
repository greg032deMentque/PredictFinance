using BackPredictFinance.Common.enums;
using BackPredictFinance.Datas.Models;
using BackPredictFinance.ViewModels.UserViewModels;
using Microsoft.AspNet.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;


namespace BackPredictFinance.Services.UserServices
{
   
    public class UserRoleDataService : BaseService
    {

        public UserRoleDataService(IServiceProvider serviceProvider)
            : base(serviceProvider)
        {
        }



        public async Task<List<UserRoleViewModel>> GetAllRoles()
        {
            var listVm = new List<UserRoleViewModel>();
            var existingRoles = await _roleManager.Roles.ToListAsync();

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
                    case UserRoleEnum.User:
                        vm.RoleName = "Utilisateur";
                        break;

                    case UserRoleEnum.Admin:
                        vm.RoleName = "Admin";
                        break;



                    default:
                        // Fallback to the raw name if you haven't handled it
                        vm.RoleName = role.Name;
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

            // Ajoute les rôles manquants
            foreach (var role in newRoles.Except(currentRoles))
            {
                await _userManager.AddToRoleAsync(user, role);
            }

            // Supprime les rôles obsolètes
            foreach (var role in currentRoles.Except(newRoles))
            {
                await _userManager.RemoveFromRoleAsync(user, role);
            }
        }


        /// <summary>
        /// check role
        /// </summary>
        /// <param name="userEmail"></param>
        /// <param name="role"></param>
        /// <returns></returns>
        public async Task<bool> IsUserInRole(string userEmail, UserRoleViewModel rolesVm)
        {
            var user = await _userManager.FindByEmailAsync(userEmail);
            return user != null && await _userManager.IsInRoleAsync(user, rolesVm.RoleName);
        }


        /// <summary>
        /// créé une liste de role pour un user. Le nom des roles est formatés.
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
                        case UserRoleEnum.User:
                            vm.RoleName = "Utilisateur";
                            break;

                        case UserRoleEnum.Admin:
                            vm.RoleName = "Admin";
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
