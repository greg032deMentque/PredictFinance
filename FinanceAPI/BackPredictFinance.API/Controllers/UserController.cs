using BackPredictFinance.Services.UserServices;
using BackPredictFinance.ViewModels.UserViewModels;
using BackPredictFinance.ViewModels.WebViewModels.PaginateViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BackPredictFinance.API.Controllers
{
    [Authorize(Policy = "Bearer")]
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly IUserRoleDataService _userRoleDataService;

        public UserController(IUserService userService, IUserRoleDataService userRoleDataService)
        {
            _userService = userService;
            _userRoleDataService = userRoleDataService;
        }

        [HttpPost("GetUsersList")]
        [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<IActionResult> GetUsers(
            [FromBody] PaginateSettingsViewModel settings,
            CancellationToken ct = default)
        {

            var users = await _userService.GetUsersPaged(settings, ct);
            return Ok(users);
        }

        [HttpGet("GetUserById")]
        public async Task<ActionResult> GetUserById([FromQuery] string userId, CancellationToken ct = default)
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

        [HttpPut("UpdateUser")]
        public async Task<ActionResult> UpdateUser([FromBody] UserViewModel userVM, CancellationToken ct = default)
        {
            var result = await _userService.UpdateUser(userVM, ct);

            return Ok(result);

        }

        [HttpDelete("DeleteUser")]
        [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<ActionResult> DeleteUser([FromQuery] string userId, CancellationToken ct = default)
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
