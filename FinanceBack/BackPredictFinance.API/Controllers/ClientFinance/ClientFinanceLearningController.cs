using BackPredictFinance.Services.ClientFinanceServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BackPredictFinance.API.Controllers.ClientFinance
{
    [Authorize(Policy = "Bearer")]
    [Route("api/ClientFinance")]
    [ApiController]
    public class ClientFinanceLearningController(
        IClientFinanceLearningService learningService,
        IClientGlossaryService clientGlossaryService,
        IEducationContentService educationContentService,
        IGlossaryTermService glossaryTermService,
        IFaqService faqService,
        ILegalCardService legalCardService) : ControllerBase
    {
        [HttpGet("learn")]
        public async Task<IActionResult> GetLearnOverview(CancellationToken ct)
        {
            return Ok(await learningService.GetLearnOverviewAsync(ct));
        }

        [HttpGet("onboarding")]
        public async Task<IActionResult> GetOnboarding(CancellationToken ct)
        {
            return Ok(await learningService.GetOnboardingGuidanceAsync(ct));
        }

        [HttpGet("glossary")]
        public async Task<IActionResult> GetGlossary(CancellationToken ct)
        {
            return Ok(await clientGlossaryService.GetGlossaryAsync(ct));
        }

        [HttpGet("education")]
        public async Task<IActionResult> GetEducationArticles(CancellationToken ct)
        {
            return Ok(await educationContentService.GetPublishedAsync(ct));
        }

        [HttpGet("education/{slug}")]
        public async Task<IActionResult> GetEducationArticle([FromRoute] string slug, CancellationToken ct)
        {
            var article = await educationContentService.GetBySlugAsync(slug, ct);
            return article is null ? NotFound() : Ok(article);
        }

        [HttpGet("glossary-terms")]
        public async Task<IActionResult> GetGlossaryTerms([FromQuery] string? search, CancellationToken ct)
        {
            return Ok(await glossaryTermService.SearchAsync(search, ct));
        }

        [HttpGet("faq")]
        public async Task<IActionResult> GetFaq(CancellationToken ct)
            => Ok(await faqService.GetPublishedAsync(ct));

        [HttpGet("legal-cards")]
        public async Task<IActionResult> GetLegalCards(CancellationToken ct)
            => Ok(await legalCardService.GetPublishedAsync(ct));
    }
}
