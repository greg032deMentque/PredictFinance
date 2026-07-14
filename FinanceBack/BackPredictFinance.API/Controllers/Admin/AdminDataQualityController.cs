using BackPredictFinance.Services.AdminGovernance;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BackPredictFinance.API.Controllers.Admin
{
    [Authorize(Policy = "RequireAdminRole")]
    [Route("api/admin")]
    [ApiController]
    public sealed class AdminDataQualityController : ControllerBase
    {
        private readonly IAdminDataQualityService _adminDataQualityService;

        public AdminDataQualityController(IAdminDataQualityService adminDataQualityService)
        {
            _adminDataQualityService = adminDataQualityService;
        }

        [HttpGet("data-quality")]
        public async Task<IActionResult> GetDataQuality(CancellationToken ct)
        {
            return Ok(await _adminDataQualityService.GetAsync(ct));
        }
    }
}
