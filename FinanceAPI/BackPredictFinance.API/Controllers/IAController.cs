using BackPredictFinance.Services.PythonServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BackPredictFinance.API.Controllers
{
    [Authorize(Policy = "Bearer")]
    [Route("[controller]")]
    [ApiController]
    public sealed class IAController : ControllerBase
    {
        private readonly IIAStatusService _iaStatusService;

        public IAController(IIAStatusService iaStatusService)
        {
            _iaStatusService = iaStatusService;
        }

        [HttpGet("Health")]
        public async Task<IActionResult> Health(CancellationToken ct)
        {
            var result = await _iaStatusService.GetHealthAsync(ct);
            return Ok(result);
        }

        [Authorize(Policy = "RequireAdminRole")]
        [HttpGet("Status")]
        public async Task<IActionResult> Status(CancellationToken ct)
        {
            var result = await _iaStatusService.GetStatusAsync(ct);
            return Ok(result);
        }
    }
}
