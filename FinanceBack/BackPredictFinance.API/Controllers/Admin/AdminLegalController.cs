using BackPredictFinance.Services.ClientFinanceServices;
using BackPredictFinance.ViewModels.AdminViewModels.Content;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BackPredictFinance.API.Controllers.Admin
{
    [Authorize(Policy = "RequireAdminRole")]
    [Route("api/admin")]
    [ApiController]
    public sealed class AdminLegalController(ILegalCardService legalCardService) : ControllerBase
    {
        [HttpGet("legal-cards")]
        public async Task<IActionResult> GetAll(CancellationToken ct)
            => Ok(await legalCardService.GetAllAdminAsync(ct));

        [HttpGet("legal-cards/{id}")]
        public async Task<IActionResult> GetById([FromRoute] string id, CancellationToken ct)
        {
            var result = await legalCardService.GetByIdAsync(id, ct);
            return result is null ? NotFound() : Ok(result);
        }

        [HttpPost("legal-cards")]
        public async Task<IActionResult> Create([FromBody] LegalCardUpsertRequestViewModel request, CancellationToken ct)
        {
            var created = await legalCardService.CreateAsync(request, ct);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }

        [HttpPut("legal-cards/{id}")]
        public async Task<IActionResult> Update([FromRoute] string id, [FromBody] LegalCardUpsertRequestViewModel request, CancellationToken ct)
            => Ok(await legalCardService.UpdateAsync(id, request, ct));

        [HttpDelete("legal-cards/{id}")]
        public async Task<IActionResult> Delete([FromRoute] string id, CancellationToken ct)
        {
            await legalCardService.DeleteAsync(id, ct);
            return NoContent();
        }
    }
}
