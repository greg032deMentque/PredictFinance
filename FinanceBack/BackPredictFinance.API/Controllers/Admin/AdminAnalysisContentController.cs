using BackPredictFinance.Services.ClientFinanceServices;
using BackPredictFinance.ViewModels.AdminViewModels.Content;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BackPredictFinance.API.Controllers.Admin
{
    [Authorize(Policy = "RequireAdminRole")]
    [Route("api/admin")]
    [ApiController]
    public sealed class AdminAnalysisContentController(IAnalysisContentService analysisContentService) : ControllerBase
    {
        [HttpGet("pattern-definitions")]
        public async Task<IActionResult> GetPatterns(CancellationToken ct)
            => Ok(await analysisContentService.GetPatternsAsync(ct));

        [HttpGet("pattern-definitions/{patternId}")]
        public async Task<IActionResult> GetPatternById([FromRoute] string patternId, CancellationToken ct)
        {
            var result = await analysisContentService.GetPatternByIdAsync(patternId, ct);
            return result is null ? NotFound() : Ok(result);
        }

        [HttpPut("pattern-definitions/{patternId}")]
        public async Task<IActionResult> UpdatePattern([FromRoute] string patternId, [FromBody] PatternDefinitionUpdateRequestViewModel request, CancellationToken ct)
            => Ok(await analysisContentService.UpdatePatternAsync(patternId, request, ct));

        [HttpGet("analysis-concepts")]
        public async Task<IActionResult> GetConcepts(CancellationToken ct)
            => Ok(await analysisContentService.GetConceptsAsync(ct));

        [HttpGet("analysis-concepts/{code}")]
        public async Task<IActionResult> GetConceptByCode([FromRoute] string code, CancellationToken ct)
        {
            var result = await analysisContentService.GetConceptByCodeAsync(code, ct);
            return result is null ? NotFound() : Ok(result);
        }

        [HttpPost("analysis-concepts")]
        public async Task<IActionResult> CreateConcept([FromBody] AnalysisConceptCreateRequestViewModel request, CancellationToken ct)
        {
            var created = await analysisContentService.CreateConceptAsync(request, ct);
            return CreatedAtAction(nameof(GetConceptByCode), new { code = created.Code }, created);
        }

        [HttpPut("analysis-concepts/{code}")]
        public async Task<IActionResult> UpdateConcept([FromRoute] string code, [FromBody] AnalysisConceptUpdateRequestViewModel request, CancellationToken ct)
            => Ok(await analysisContentService.UpdateConceptAsync(code, request, ct));

        [HttpDelete("analysis-concepts/{code}")]
        public async Task<IActionResult> DeleteConcept([FromRoute] string code, CancellationToken ct)
        {
            await analysisContentService.DeleteConceptAsync(code, ct);
            return NoContent();
        }
    }
}
