using BackPredictFinance.Services.AuthServices;
using BackPredictFinance.Services.UserServices;
using BackPredictFinance.ViewModels.UserViewModels;
using BackPredictFinance.ViewModels.UserViewModels.AuthViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;



namespace BackPredictFinance.API.Controllers
{
    [Authorize(Policy = "Bearer")]
    [Route("api/[controller]")]
    [ApiController]
    public class AccountController : ControllerBase
    {
        private readonly IAccountService _accountService;
        private readonly ICurrentUserSessionService _currentUserSessionService;
        private readonly IUserService _userService;
        private readonly IUserPrivacyService _userPrivacyService;

        public AccountController(
            IAccountService accountService,
            ICurrentUserSessionService currentUserSessionService,
            IUserService userService,
            IUserPrivacyService userPrivacyService)
        {
            _accountService = accountService;
            _currentUserSessionService = currentUserSessionService;
            _userService = userService;
            _userPrivacyService = userPrivacyService;
        }

        [AllowAnonymous]
        [HttpPost("Register")]
        public async Task<IActionResult> Register([FromBody] PublicSignupRequestViewModel model, CancellationToken ct = default)
        {
            var createdAccount = await _userService.RegisterPublic(model, ct);
            return StatusCode(StatusCodes.Status201Created, createdAccount);
        }

        [AllowAnonymous]
        [HttpPost("ConfirmEmail")]
        public async Task<IActionResult> ConfirmEmail([FromBody] ConfirmEmailViewModel model)
        {
            await _accountService.ConfirmEmailAsync(model);
            return NoContent();
        }

        [AllowAnonymous]
        [HttpPost("ResendConfirmationEmail")]
        public async Task<IActionResult> ResendConfirmationEmail([FromBody] ResendConfirmationEmailViewModel model)
        {
            await _accountService.ResendConfirmationEmailAsync(model.Email);
            return NoContent();
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


        [HttpGet("Profile")]
        public async Task<IActionResult> Profile(CancellationToken ct = default)
        {
            var profile = await _userService.GetCurrentUserProfile(ct);
            return Ok(profile);
        }

        [HttpPut("Profile")]
        public async Task<IActionResult> Profile([FromBody] UpdateCurrentUserProfileRequestViewModel model, CancellationToken ct = default)
        {
            var profile = await _userService.UpdateCurrentUserProfile(model, ct);
            return Ok(profile);
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
        public async Task<IActionResult> Logout([FromBody] TokenViewModel? model)
        {
            await _accountService.Logout(model);
            return Ok();
        }

        [HttpGet("me")]
        public async Task<IActionResult> Me(CancellationToken ct = default)
        {
            var currentUser = await _currentUserSessionService.GetCurrentAsync(ct);
            if (currentUser is null)
            {
                return Unauthorized();
            }

            return Ok(new CurrentUserViewModel
            {
                UserId = currentUser.UserId,
                DisplayName = currentUser.DisplayName,
                Email = currentUser.Email,
                Roles = currentUser.Roles,
                AllowedAreas = currentUser.AllowedAreas
            });
        }

        [HttpGet("consents")]
        public async Task<IActionResult> GetConsents(CancellationToken ct)
        {
            return Ok(await _userPrivacyService.GetConsentsAsync(ct));
        }

        [HttpPatch("consents")]
        public async Task<IActionResult> UpdateConsents([FromBody] UpdateUserConsentsRequestViewModel model, CancellationToken ct)
        {
            return Ok(await _userPrivacyService.UpdateConsentsAsync(model, ct));
        }

        [HttpPost("data-export")]
        public async Task<IActionResult> RequestDataExport(CancellationToken ct)
        {
            return Ok(await _userPrivacyService.RequestDataExportAsync(ct));
        }

        [HttpDelete("self")]
        public async Task<IActionResult> DeleteAccount([FromBody] DeleteAccountRequestViewModel model, CancellationToken ct)
        {
            await _userPrivacyService.DeleteCurrentAccountAsync(model, ct);
            return NoContent();
        }

        [HttpGet("alert-preferences")]
        public async Task<IActionResult> GetAlertPreferences(CancellationToken ct)
        {
            return Ok(await _userPrivacyService.GetAlertPreferencesAsync(ct));
        }

        [HttpPatch("alert-preferences")]
        public async Task<IActionResult> UpdateAlertPreferences([FromBody] UpdateAlertPreferencesRequestViewModel model, CancellationToken ct)
        {
            return Ok(await _userPrivacyService.UpdateAlertPreferencesAsync(model, ct));
        }
    }
}
