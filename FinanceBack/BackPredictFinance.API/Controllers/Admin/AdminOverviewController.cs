using BackPredictFinance.Services.AdminGovernance;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BackPredictFinance.API.Controllers.Admin
{
    [Authorize(Policy = "RequireAdminRole")]
    [Route("api/admin")]
    [ApiController]
    public sealed class AdminOverviewController : ControllerBase
    {
        private readonly IAdminOverviewService _adminOverviewService;

        public AdminOverviewController(IAdminOverviewService adminOverviewService)
        {
            _adminOverviewService = adminOverviewService;
        }

        [HttpGet("overview")]
        public async Task<IActionResult> GetOverview(CancellationToken ct)
        {
            return Ok(await _adminOverviewService.GetAsync(ct));
        }
    }
}
