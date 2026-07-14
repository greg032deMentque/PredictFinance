using BackPredictFinance.Services.ClientFinanceServices;
using BackPredictFinance.ViewModels.AdminViewModels.Content;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BackPredictFinance.API.Controllers.Admin
{
    [Authorize(Policy = "RequireAdminRole")]
    [Route("api/admin")]
    [ApiController]
    public sealed class AdminLearnTopicController(ILearnTopicService learnTopicService) : ControllerBase
    {
        [HttpGet("learn-topics")]
        public async Task<IActionResult> GetAll(CancellationToken ct)
            => Ok(await learnTopicService.GetAllAdminAsync(ct));

        [HttpGet("learn-topics/{id}")]
        public async Task<IActionResult> GetById([FromRoute] string id, CancellationToken ct)
        {
            var result = await learnTopicService.GetByIdAsync(id, ct);
            return result is null ? NotFound() : Ok(result);
        }

        [HttpPost("learn-topics")]
        public async Task<IActionResult> Create([FromBody] LearnTopicUpsertRequestViewModel request, CancellationToken ct)
        {
            var created = await learnTopicService.CreateAsync(request, ct);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }

        [HttpPut("learn-topics/{id}")]
        public async Task<IActionResult> Update([FromRoute] string id, [FromBody] LearnTopicUpsertRequestViewModel request, CancellationToken ct)
            => Ok(await learnTopicService.UpdateAsync(id, request, ct));

        [HttpDelete("learn-topics/{id}")]
        public async Task<IActionResult> Delete([FromRoute] string id, CancellationToken ct)
        {
            await learnTopicService.DeleteAsync(id, ct);
            return NoContent();
        }
    }
}
