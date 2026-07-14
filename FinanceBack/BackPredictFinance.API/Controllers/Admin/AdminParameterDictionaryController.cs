using BackPredictFinance.Services.AdminGovernance;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BackPredictFinance.API.Controllers.Admin
{
    [Authorize(Policy = "RequireAdminRole")]
    [Route("api/admin")]
    [ApiController]
    public sealed class AdminParameterDictionaryController : ControllerBase
    {
        private readonly IAdminParameterDictionaryAdminService _adminParameterDictionaryAdminService;

        public AdminParameterDictionaryController(IAdminParameterDictionaryAdminService adminParameterDictionaryAdminService)
        {
            _adminParameterDictionaryAdminService = adminParameterDictionaryAdminService;
        }

        [HttpGet("parameter-dictionary")]
        public async Task<IActionResult> GetAll(CancellationToken ct)
        {
            return Ok(await _adminParameterDictionaryAdminService.GetListAsync(ct));
        }

        [HttpGet("parameter-dictionary/{parameterId}")]
        public async Task<IActionResult> GetById([FromRoute] string parameterId, CancellationToken ct)
        {
            return Ok(await _adminParameterDictionaryAdminService.GetDetailAsync(parameterId, ct));
        }
    }
}
