using AutoMapper;
using BackPredictFinance.Services.AdminGovernance;
using BackPredictFinance.ViewModels.AdminViewModels.Wording;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BackPredictFinance.API.Controllers.Admin
{
    [Authorize(Policy = "RequireAdminRole")]
    [Route("api/admin")]
    [ApiController]
    public sealed class AdminWordingVersionsController : ControllerBase
    {
        private readonly IAdminWordingVersionService _adminWordingVersionService;
        private readonly IMapper _mapper;

        public AdminWordingVersionsController(IAdminWordingVersionService adminWordingVersionService, IMapper mapper)
        {
            _adminWordingVersionService = adminWordingVersionService;
            _mapper = mapper;
        }

        [HttpGet("wording-versions")]
        public async Task<IActionResult> GetAll(CancellationToken ct)
        {
            var payload = await _adminWordingVersionService.GetListAsync(ct);
            return Ok(_mapper.Map<List<AdminWordingVersionListItemViewModel>>(payload));
        }

        [HttpGet("wording-scenarios/{scenarioCode}")]
        public async Task<IActionResult> GetScenario([FromRoute] string scenarioCode, CancellationToken ct)
        {
            var payload = await _adminWordingVersionService.GetDetailAsync(scenarioCode, ct);
            return Ok(_mapper.Map<AdminWordingVersionDetailViewModel>(payload));
        }

        [HttpGet("wording-versions/{wordingVersionId}")]
        public async Task<IActionResult> GetById([FromRoute] string wordingVersionId, CancellationToken ct)
        {
            var payload = await _adminWordingVersionService.GetVersionDetailAsync(wordingVersionId, ct);

            return Ok(new AdminWordingVersionVersionDetailViewModel
            {
                WordingVersionId = payload.WordingVersionId,
                DisplayName = payload.DisplayName,
                PublicationState = new WordingPublicationStateViewModel
                {
                    IsActive = payload.PublicationState.IsActive,
                    ActivatedAtUtc = payload.PublicationState.ActivatedAtUtc,
                    RecommendationPolicyVersion = payload.PublicationState.RecommendationPolicyVersion,
                    ExplanationPolicyVersion = payload.PublicationState.ExplanationPolicyVersion,
                    AffectedDomains = payload.PublicationState.AffectedDomains
                },
                Scenarios = payload.Scenarios.Select(scenario => new WordingScenarioTemplateSummaryViewModel
                {
                    ScenarioCode = scenario.ScenarioCode,
                    RecommendationKind = scenario.RecommendationKind,
                    HoldingStatus = scenario.HoldingStatus,
                    ActionVerbFamilyCode = scenario.ActionVerbFamilyCode,
                    SupportedStrengths = scenario.SupportedStrengths,
                    TemplateSummary = scenario.TemplateSummary
                }).ToList()
            });
        }
    }
}
