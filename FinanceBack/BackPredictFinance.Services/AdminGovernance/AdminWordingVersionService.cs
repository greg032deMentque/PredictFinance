using Microsoft.EntityFrameworkCore;

namespace BackPredictFinance.Services.AdminGovernance
{
    public interface IAdminWordingVersionService
    {
        /// <summary>
        /// Retourne la liste des versions de wording gouvernees avec leur etat de publication.
        /// </summary>
        Task<List<AdminWordingVersionListItem>> GetListAsync(CancellationToken ct = default);

        /// <summary>
        /// Retourne le detail gouverne d'un scenario de wording actif.
        /// </summary>
        Task<AdminWordingVersionDetail> GetDetailAsync(string scenarioCode, CancellationToken ct = default);

        /// <summary>
        /// Retourne le detail gouverne d'une version de wording avec l'ensemble de ses scenarios.
        /// </summary>
        Task<AdminWordingVersionVersionDetail> GetVersionDetailAsync(string wordingVersionId, CancellationToken ct = default);
    }

    public sealed class AdminWordingVersionService : BaseService, IAdminWordingVersionService
    {
        private readonly IWordingPublicationService _wordingPublicationService;

        public AdminWordingVersionService(IServiceProvider serviceProvider, IWordingPublicationService wordingPublicationService) : base(serviceProvider)
        {
            _wordingPublicationService = wordingPublicationService;
        }

        public async Task<List<AdminWordingVersionListItem>> GetListAsync(CancellationToken ct = default)
        {
            return await _financeDbContext.RecommendationWordingVersions
                .AsNoTracking()
                .OrderByDescending(x => x.IsActive)
                .ThenByDescending(x => x.ActivatedAtUtc)
                .ThenBy(x => x.WordingVersionId)
                .Select(x => new AdminWordingVersionListItem
                {
                    WordingVersionId = x.WordingVersionId,
                    DisplayName = x.DisplayName,
                    IsActive = x.IsActive,
                    ActivatedAtUtc = x.ActivatedAtUtc,
                    ScenarioCount = x.Scenarios.Count,
                    PublicationState = new WordingPublicationState
                    {
                        IsActive = x.IsActive,
                        ActivatedAtUtc = x.ActivatedAtUtc,
                        RecommendationPolicyVersion = x.RecommendationPolicyVersion,
                        ExplanationPolicyVersion = x.ExplanationPolicyVersion,
                        AffectedDomains = IWordingPublicationServiceSplitCsv(x.AffectedDomains)
                    }
                })
                .ToListAsync(ct);
        }

        public async Task<AdminWordingVersionDetail> GetDetailAsync(string scenarioCode, CancellationToken ct = default)
        {
            var normalizedScenarioCode = (scenarioCode ?? string.Empty).Trim().ToUpperInvariant();
            if (string.IsNullOrWhiteSpace(normalizedScenarioCode))
            {
                throw new ArgumentException("Scenario code is required.", nameof(scenarioCode));
            }

            var publicationState = await _wordingPublicationService.GetActivePublicationAsync(ct)
                ?? throw new KeyNotFoundException("No active wording publication is available.");

            var scenario = await _financeDbContext.RecommendationWordingScenarios
                .AsNoTracking()
                .Where(x => x.WordingVersion.IsActive && x.ScenarioCode == normalizedScenarioCode)
                .Select(x => new
                {
                    x.WordingVersionId,
                    x.WordingVersion.DisplayName,
                    Scenario = x
                })
                .FirstOrDefaultAsync(ct);

            if (scenario is null)
            {
                throw new KeyNotFoundException($"Scenario '{normalizedScenarioCode}' was not found in the active wording publication.");
            }

            return new AdminWordingVersionDetail
            {
                WordingVersionId = scenario.WordingVersionId,
                DisplayName = scenario.DisplayName,
                PublicationState = publicationState,
                Scenario = WordingPublicationService.MapScenario(scenario.Scenario)
            };
        }


        public async Task<AdminWordingVersionVersionDetail> GetVersionDetailAsync(string wordingVersionId, CancellationToken ct = default)
        {
            var normalizedWordingVersionId = (wordingVersionId ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(normalizedWordingVersionId))
            {
                throw new ArgumentException("Wording version id is required.", nameof(wordingVersionId));
            }

            var version = await _financeDbContext.RecommendationWordingVersions
                .AsNoTracking()
                .Where(x => x.WordingVersionId == normalizedWordingVersionId)
                .Select(x => new AdminWordingVersionVersionDetail
                {
                    WordingVersionId = x.WordingVersionId,
                    DisplayName = x.DisplayName,
                    PublicationState = new WordingPublicationState
                    {
                        IsActive = x.IsActive,
                        ActivatedAtUtc = x.ActivatedAtUtc,
                        RecommendationPolicyVersion = x.RecommendationPolicyVersion,
                        ExplanationPolicyVersion = x.ExplanationPolicyVersion,
                        AffectedDomains = IWordingPublicationServiceSplitCsv(x.AffectedDomains)
                    },
                    Scenarios = x.Scenarios
                        .OrderBy(s => s.ScenarioCode)
                        .Select(s => WordingPublicationService.MapScenario(s))
                        .ToList()
                })
                .FirstOrDefaultAsync(ct);

            if (version is null)
            {
                throw new KeyNotFoundException($"Wording version '{normalizedWordingVersionId}' was not found.");
            }

            return version;
        }

        private static List<string> IWordingPublicationServiceSplitCsv(string csv)
        {
            return WordingPublicationService.SplitCsv(csv);
        }
    }
}
