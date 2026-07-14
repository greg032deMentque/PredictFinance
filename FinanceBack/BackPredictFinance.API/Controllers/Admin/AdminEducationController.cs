using BackPredictFinance.Services.AdminGovernance;
using BackPredictFinance.ViewModels.AdminViewModels.Education;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BackPredictFinance.API.Controllers.Admin
{
    [Authorize(Policy = "RequireAdminRole")]
    [Route("api/admin")]
    [ApiController]
    public sealed class AdminEducationController(IAdminEducationService adminEducationService) : ControllerBase
    {
        [HttpGet("education")]
        public async Task<IActionResult> GetAll(CancellationToken ct)
            => Ok(await adminEducationService.GetAllAsync(ct));

        [HttpGet("education/{id}")]
        public async Task<IActionResult> GetById([FromRoute] string id, CancellationToken ct)
        {
            var result = await adminEducationService.GetByIdAsync(id, ct);
            return result is null ? NotFound() : Ok(result);
        }

        [HttpPost("education")]
        public async Task<IActionResult> Create([FromBody] EducationArticleUpsertRequestViewModel request, CancellationToken ct)
        {
            var created = await adminEducationService.CreateAsync(request, ct);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }

        [HttpPut("education/{id}")]
        public async Task<IActionResult> Update([FromRoute] string id, [FromBody] EducationArticleUpsertRequestViewModel request, CancellationToken ct)
            => Ok(await adminEducationService.UpdateAsync(id, request, ct));

        [HttpDelete("education/{id}")]
        public async Task<IActionResult> Delete([FromRoute] string id, CancellationToken ct)
        {
            await adminEducationService.DeleteAsync(id, ct);
            return NoContent();
        }
    }
}
