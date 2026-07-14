using BackPredictFinance.Services.AdminGovernance;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BackPredictFinance.API.Controllers.Admin
{
    [Authorize(Policy = "RequireAdminRole")]
    [Route("api/admin")]
    [ApiController]
    public sealed class AdminInstrumentRegistryController(
        IAdminInstrumentRegistryService adminInstrumentRegistryService,
        IAdminInstrumentSeedService adminInstrumentSeedService) : ControllerBase
    {
        [HttpGet("instruments")]
        public async Task<IActionResult> GetAll(CancellationToken ct)
        {
            return Ok(await adminInstrumentRegistryService.GetListAsync(ct));
        }

        [HttpGet("instruments/{assetId}")]
        public async Task<IActionResult> GetById([FromRoute] string assetId, CancellationToken ct)
        {
            return Ok(await adminInstrumentRegistryService.GetDetailAsync(assetId, ct));
        }

        
        [HttpPost("instruments/seed")]
        public async Task<IActionResult> Seed(CancellationToken ct)
        {
            return Ok(await adminInstrumentSeedService.SeedAsync(ct));
        }
    }
}
