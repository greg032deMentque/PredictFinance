using BackPredictFinance.Services.AdminGovernance;
using BackPredictFinance.ViewModels.AdminViewModels.ScoringPolicy;
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

        [HttpGet("scoring-policy-versions")]
        public async Task<IActionResult> GetVersions(CancellationToken ct)
        {
            return Ok(await _adminScoringPolicyService.GetVersionsAsync(ct));
        }

        [HttpGet("scoring-policy-versions/{id}")]
        public async Task<IActionResult> GetVersionById([FromRoute] string id, CancellationToken ct)
        {
            return Ok(await _adminScoringPolicyService.GetVersionByIdAsync(id, ct));
        }

        [HttpPost("scoring-policy-versions")]
        public async Task<IActionResult> CreateVersion([FromBody] AdminScoringPolicyVersionCreateRequestViewModel request, CancellationToken ct)
        {
            var created = await _adminScoringPolicyService.CreateVersionAsync(request, ct);
            return CreatedAtAction(nameof(GetVersionById), new { id = created.Id }, created);
        }

        [HttpPost("scoring-policy-versions/{id}/activate")]
        public async Task<IActionResult> ActivateVersion([FromRoute] string id, CancellationToken ct)
        {
            return Ok(await _adminScoringPolicyService.ActivateVersionAsync(id, ct));
        }
    }
}
