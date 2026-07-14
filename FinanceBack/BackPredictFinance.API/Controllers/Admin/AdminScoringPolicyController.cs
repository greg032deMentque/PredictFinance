using BackPredictFinance.Services.AdminGovernance;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BackPredictFinance.API.Controllers.Admin
{
    [Authorize(Policy = "RequireAdminRole")]
    [Route("api/admin")]
    [ApiController]
    public sealed class AdminScoringPolicyController : ControllerBase
    {
        private readonly IAdminScoringPolicyService _adminScoringPolicyService;

        public AdminScoringPolicyController(IAdminScoringPolicyService adminScoringPolicyService)
        {
            _adminScoringPolicyService = adminScoringPolicyService;
        }

        [HttpGet("scoring-policy")]
        public async Task<IActionResult> GetScoringPolicy(CancellationToken ct)
        {
            return Ok(await _adminScoringPolicyService.GetAsync(ct));
        }
    }
}
