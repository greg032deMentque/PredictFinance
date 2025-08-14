using BackPredictFinance.Services;
using BackPredictFinance.Services.UserServices;
using BackPredictFinance.ViewModels.CommonViewModels;
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
        private readonly AccountService _accountService;
        private readonly UserService _userService;
        private readonly IMemoryCache _cache;

        public AccountController(
          AccountService accountService, UserService userService, IMemoryCache cache)
        {
            _accountService = accountService;
            _userService = userService;
            _cache = cache;

        }

        [AllowAnonymous]
        [HttpGet("ForgotPassword")]
        public async Task<IActionResult> ForgotPassword(string email)
        {
           await _accountService.ForgotPassword(email);

            return Ok();

        }

        [AllowAnonymous]
        [HttpPost("ResetPassword")]
        public async Task<IActionResult> ResetPassword(ResetPasswordPayloadViewModel model)
        {
            await _accountService.ResetPassword(model);

            return Ok();
        }

        [HttpPost("ChangePassword")]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel resetPassword)
        {
            await _accountService.ChangePassword(resetPassword);

            return Ok();
        }

        [AllowAnonymous]
        [HttpPost("Login")]
        public async Task<IActionResult> Login([FromBody] LoginViewModel model)
        {
            var result = await _accountService.Login(model);

            return Ok(result);
        }


        [HttpPost("Logout")]
        public async Task<IActionResult> Logout()
        {
            await _accountService.Logout();

            return Ok();
        }

        [Authorize(Roles = "Admin,SuperAdmin")]
        [HttpPost("UnlockUser/{userId}")]
        public async Task<IActionResult> UnlockUser(string userId)
        {
            await _accountService.UnlockUser(userId);
            return Ok();
        }

        /// <summary>
        /// Si l'IP a été bloquée
        /// </summary>
        /// <param name="ipAddress"></param>
        /// <returns></returns>
        [HttpDelete("UnblockIp/{ipAddress}")]
        public IActionResult UnblockIP(string ipAddress)
        {
            var key = $"RateLimit_{ipAddress}";
            _cache.Remove(key);
            return Ok($"L'IP {ipAddress} a été débloquée.");
        }
    }

}
