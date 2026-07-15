using BackPredictFinance.Services.ClientFinanceServices.Screener;
using BackPredictFinance.ViewModels.ClientFinanceViewModels.Screener;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BackPredictFinance.API.Controllers.ClientFinance
{
    [Authorize(Policy = "Bearer")]
    [Route("api/ClientFinance")]
    [ApiController]
    public class ClientFinanceScreenerController(IScreenerService screenerService) : ControllerBase
    {
        [HttpGet("screener")]
        public async Task<IActionResult> GetScreener([FromQuery] ScreenerQueryViewModel query, CancellationToken ct)
        {
            var result = await screenerService.GetPagedAsync(query, ct);
            return Ok(result);
        }

        [HttpGet("screener/meta")]
        public async Task<IActionResult> GetScreenerMeta(CancellationToken ct)
        {
            var result = await screenerService.GetMetaAsync(ct);
            return Ok(result);
        }

        [HttpGet("screener/export")]
        public async Task<IActionResult> ExportScreener([FromQuery] ScreenerQueryViewModel query, CancellationToken ct)
        {
            var csv = await screenerService.ExportCsvAsync(query, ct);
            return File(csv, "text/csv", "screener-export.csv");
        }

        [HttpGet("screener/presets")]
        public async Task<IActionResult> GetScreenerPresets(CancellationToken ct)
        {
            var result = await screenerService.GetPresetsAsync(ct);
            return Ok(result);
        }

        [HttpPost("screener/presets")]
        public async Task<IActionResult> CreateScreenerPreset([FromBody] ScreenerPresetCreateViewModel model, CancellationToken ct)
        {
            var created = await screenerService.SavePresetAsync(model, ct);
            return CreatedAtAction(nameof(GetScreenerPresets), created);
        }

        [HttpDelete("screener/presets/{id}")]
        public async Task<IActionResult> DeleteScreenerPreset([FromRoute] string id, CancellationToken ct)
        {
            await screenerService.DeletePresetAsync(id, ct);
            return NoContent();
        }
    }
}
