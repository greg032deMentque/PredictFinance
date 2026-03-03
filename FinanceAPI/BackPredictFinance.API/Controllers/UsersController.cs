using BackPredictFinance.Services.UserServices;
using BackPredictFinance.ViewModels.UserViewModels;
using BackPredictFinance.ViewModels.WebViewModels.PaginateViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BackPredictFinance.API.Controllers
{
    [Authorize(Policy = "Bearer")]
    [Route("[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly IUserRoleDataService _userRoleDataService;

        public UsersController(IUserService userService, IUserRoleDataService userRoleDataService)
        {
            _userService = userService;
            _userRoleDataService = userRoleDataService;
        }


        [HttpGet("GetUsersList")]
        [Authorize(Roles = "Admin,SuperAdmin")]

        public async Task<IActionResult> GetUsers(
          PaginateSettingsViewModel settings,
            CancellationToken ct = default)
        {

            var users = await _userService.GetUsersPaged(settings, ct);
            return Ok(users);
        }

        [HttpGet("GetUserDatas")]
        public async Task<ActionResult> GetUserDetails(string userId, CancellationToken ct = default)
        {
            var result = await _userService.GetUserDetails(userId, ct);
            return Ok(result);
        }


        [HttpPost("CreateUser")]
        [Authorize(Roles = "Admin,SuperAdmin")]

        public async Task<IActionResult> CreateUser([FromBody] AdminUserUpsertViewModel request, CancellationToken ct = default)
        {
            var createdUser = await _userService.CreateUserAdmin(request, ct);
            return Ok(createdUser);
        }

        [HttpPut("UpdateUserDatas")]
        public async Task<ActionResult> UpdateUserDatas(UserViewModel userVM, CancellationToken ct = default)
        {
            var result = await _userService.UpsertProfilData(userVM, ct);

            return Ok(result);

        }

        [HttpDelete("DeleteUser")]
        [Authorize(Roles = "Admin,SuperAdmin")]

        public async Task<ActionResult> DeleteUser(string userId, CancellationToken ct = default)
        {
            await _userService.DeleteUser(userId, ct);

            return Ok();

        }

        [HttpGet("GetUserRoles")]
        [Authorize(Roles = "Admin,SuperAdmin")]

        public async Task<ActionResult> GetUserRoles(CancellationToken ct = default)
        {
            var rolesVm = await _userRoleDataService.GetAllRoles(ct);

            return Ok(rolesVm);

        }
    }
}
