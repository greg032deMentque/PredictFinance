using BackPredictFinance.Services.ClientFinanceServices;
using BackPredictFinance.ViewModels.AdminViewModels.Content;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BackPredictFinance.API.Controllers.Admin
{
    [Authorize(Policy = "RequireAdminRole")]
    [Route("api/admin")]
    [ApiController]
    public sealed class AdminFaqController(IFaqService faqService) : ControllerBase
    {
        [HttpGet("faq")]
        public async Task<IActionResult> GetAll(CancellationToken ct)
            => Ok(await faqService.GetAllAdminAsync(ct));

        [HttpGet("faq/{id}")]
        public async Task<IActionResult> GetById([FromRoute] string id, CancellationToken ct)
        {
            var result = await faqService.GetByIdAsync(id, ct);
            return result is null ? NotFound() : Ok(result);
        }

        [HttpPost("faq")]
        public async Task<IActionResult> Create([FromBody] FaqEntryUpsertRequestViewModel request, CancellationToken ct)
        {
            var created = await faqService.CreateAsync(request, ct);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }

        [HttpPut("faq/{id}")]
        public async Task<IActionResult> Update([FromRoute] string id, [FromBody] FaqEntryUpsertRequestViewModel request, CancellationToken ct)
            => Ok(await faqService.UpdateAsync(id, request, ct));

        [HttpDelete("faq/{id}")]
        public async Task<IActionResult> Delete([FromRoute] string id, CancellationToken ct)
        {
            await faqService.DeleteAsync(id, ct);
            return NoContent();
        }
    }
}
