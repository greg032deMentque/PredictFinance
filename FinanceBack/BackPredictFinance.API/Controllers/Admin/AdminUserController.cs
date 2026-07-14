using BackPredictFinance.Services.AuthServices;
using BackPredictFinance.Services.UserServices;
using BackPredictFinance.ViewModels.UserViewModels;
using BackPredictFinance.ViewModels.WebViewModels.PaginateViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;

namespace BackPredictFinance.API.Controllers.Admin
{
    [Authorize(Policy = "RequireAdminRole")]
    [Route("api/admin")]
    [ApiController]
    public sealed class AdminUserController : ControllerBase
    {
        private readonly IAccountService _accountService;
        private readonly IMemoryCache _cache;
        private readonly IUserAdminService _userAdminService;
        private readonly IUserRoleDataService _userRoleDataService;

        public AdminUserController(
            IUserAdminService userAdminService,
            IUserRoleDataService userRoleDataService,
            IAccountService accountService,
            IMemoryCache cache)
        {
            _userAdminService = userAdminService;
            _userRoleDataService = userRoleDataService;
            _accountService = accountService;
            _cache = cache;
        }

        [HttpPost("users/search")]
        public async Task<IActionResult> SearchUsers(
            [FromBody] PaginateSettingsViewModel settings,
            CancellationToken ct = default)
        {
            var users = await _userAdminService.GetUsersPaged(settings, ct);
            return Ok(users);
        }

        [HttpGet("users/{userId}")]
        public async Task<IActionResult> GetUserById([FromRoute] string userId, CancellationToken ct = default)
        {
            var result = await _userAdminService.GetUserDetails(userId, ct);
            return Ok(result);
        }

        [HttpPost("users")]
        public async Task<IActionResult> CreateUser([FromBody] AdminUserUpsertViewModel request, CancellationToken ct = default)
        {
            var createdUser = await _userAdminService.CreateUserAdmin(request, ct);
            return Ok(createdUser);
        }

        [HttpPut("users/{userId}")]
        public async Task<IActionResult> UpdateUser(
            [FromRoute] string userId,
            [FromBody] AdminUserUpsertViewModel request,
            CancellationToken ct = default)
        {
            var result = await _userAdminService.UpdateUserAdmin(userId, request, ct);
            return Ok(result);
        }

        [HttpDelete("users/{userId}")]
        public async Task<IActionResult> DeleteUser([FromRoute] string userId, CancellationToken ct = default)
        {
            await _userAdminService.DeleteUser(userId, ct);
            return Ok();
        }

        [HttpGet("users/roles")]
        public async Task<IActionResult> GetUserRoles(CancellationToken ct = default)
        {
            var rolesVm = await _userRoleDataService.GetAllRoles(ct);
            return Ok(rolesVm);
        }

        [HttpPost("users/{userId}/unlock")]
        public async Task<IActionResult> UnlockUser([FromRoute] string userId)
        {
            await _accountService.UnlockUser(userId);
            return Ok();
        }

        [HttpDelete("security/ip-blocks/{ipAddress}")]
        public IActionResult UnblockIp([FromRoute] string ipAddress)
        {
            _cache.Remove($"RateLimit_{ipAddress}");
            return Ok($"L'IP {ipAddress} a ete debloquee.");
        }
    }
}
