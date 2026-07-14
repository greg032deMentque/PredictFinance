using BackPredictFinance.Services.AdminGovernance;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BackPredictFinance.API.Controllers.Admin
{
    [Authorize(Policy = "RequireAdminRole")]
    [Route("api/admin")]
    [ApiController]
    public sealed class AdminPeaRegistryController : ControllerBase
    {
        private readonly IAdminPeaRegistryService _adminPeaRegistryService;

        public AdminPeaRegistryController(IAdminPeaRegistryService adminPeaRegistryService)
        {
            _adminPeaRegistryService = adminPeaRegistryService;
        }

        [HttpGet("pea-registry")]
        public async Task<IActionResult> GetPeaRegistry(CancellationToken ct)
        {
            return Ok(await _adminPeaRegistryService.GetListAsync(ct));
        }
    }
}
