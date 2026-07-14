using BackPredictFinance.Services.AdminGovernance;
using BackPredictFinance.Services.ClientFinanceServices;
using BackPredictFinance.ViewModels.AdminViewModels.Education;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BackPredictFinance.API.Controllers.Admin
{
    [Authorize(Policy = "RequireAdminRole")]
    [Route("api/admin")]
    [ApiController]
    public sealed class AdminGlossaryController(
        IAdminGlossaryService adminGlossaryService,
        IGlossaryTermService glossaryTermService) : ControllerBase
    {
        [HttpGet("glossary-terms")]
        public async Task<IActionResult> GetAll([FromQuery] string? search, CancellationToken ct)
        {
            if (!string.IsNullOrWhiteSpace(search))
                return Ok(await glossaryTermService.SearchAsync(search, ct));

            return Ok(await adminGlossaryService.GetAllAsync(ct));
        }

        [HttpGet("glossary-terms/{id}")]
        public async Task<IActionResult> GetById([FromRoute] string id, CancellationToken ct)
        {
            var result = await adminGlossaryService.GetByIdAsync(id, ct);
            return result is null ? NotFound() : Ok(result);
        }

        [HttpPost("glossary-terms")]
        public async Task<IActionResult> Create([FromBody] GlossaryTermUpsertRequestViewModel request, CancellationToken ct)
        {
            var created = await adminGlossaryService.CreateAsync(request, ct);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }

        [HttpPut("glossary-terms/{id}")]
        public async Task<IActionResult> Update([FromRoute] string id, [FromBody] GlossaryTermUpsertRequestViewModel request, CancellationToken ct)
            => Ok(await adminGlossaryService.UpdateAsync(id, request, ct));

        [HttpDelete("glossary-terms/{id}")]
        public async Task<IActionResult> Delete([FromRoute] string id, CancellationToken ct)
        {
            await adminGlossaryService.DeleteAsync(id, ct);
            return NoContent();
        }
    }
}
