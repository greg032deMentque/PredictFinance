using BackPredictFinance.Services.AuthServices;
using BackPredictFinance.ViewModels.UserViewModels.AuthViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;

namespace BackPredictFinance.API.Controllers
{
    [Authorize(Policy = "Bearer")]
    [Route("api/[controller]")]
    [ApiController]
    public class AccountController : ControllerBase
    {
        private readonly IAccountService _accountService;
        private readonly IMemoryCache _cache;

        public AccountController(IAccountService accountService, IMemoryCache cache)
        {
            _accountService = accountService;
            _cache = cache;
        }

        [AllowAnonymous]
        [HttpGet("ForgotPassword")]
        public async Task<IActionResult> ForgotPassword([FromQuery] string email)
        {
            await _accountService.ForgotPassword(email);
            return Ok();
        }

        [AllowAnonymous]
        [HttpPost("ForgotPassword")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordViewModel model)
        {
            await _accountService.ForgotPassword(model.Email);
            return Ok();
        }

        [AllowAnonymous]
        [HttpPost("ResetPassword")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequestViewModel model)
        {
            var result = await _accountService.ResetPassword(model);
            if (!result.Succeeded)
            {
                return BadRequest(result.Errors.Select(x => x.Description));
            }

            return Ok();
        }

        [HttpPost("ChangePassword")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordViewModel model)
        {
            await _accountService.ChangePassword(model);
            return Ok();
        }

        [AllowAnonymous]
        [HttpPost("Login")]
        public async Task<IActionResult> Login([FromBody] LoginViewModel model)
        {
            var result = await _accountService.Login(model);
            return result is null ? Unauthorized() : Ok(result);
        }

        [AllowAnonymous]
        [HttpPost("LoginAdmin")]
        public async Task<IActionResult> LoginAdmin([FromBody] LoginViewModel model)
        {
            var result = await _accountService.LoginAdmin(model);
            return result is null ? Unauthorized() : Ok(result);
        }

        [AllowAnonymous]
        [HttpPost("Refresh")]
        public async Task<IActionResult> Refresh([FromBody] TokenViewModel model)
        {
            var result = await _accountService.RefreshTokenAsync(model);
            return result is null ? Unauthorized() : Ok(result);
        }

        [AllowAnonymous]
        [HttpPost("Logout")]
        public async Task<IActionResult> Logout()
        {
            await _accountService.Logout();
            return Ok();
        }

        [Authorize(Roles = "Admin,SuperAdmin")]
        [HttpPost("UnlockUser/{userId}")]
        public async Task<IActionResult> UnlockUser([FromRoute] string userId)
        {
            await _accountService.UnlockUser(userId);
            return Ok();
        }

        [HttpDelete("UnblockIp/{ipAddress}")]
        public IActionResult UnblockIp([FromRoute] string ipAddress)
        {
            _cache.Remove($"RateLimit_{ipAddress}");
            return Ok($"L'IP {ipAddress} a ete debloquee.");
        }
    }
}
