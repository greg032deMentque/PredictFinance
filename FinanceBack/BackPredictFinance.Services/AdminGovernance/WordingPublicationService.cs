using BackPredictFinance.Common.enums;
using BackPredictFinance.Datas.Entities;
using Microsoft.EntityFrameworkCore;

namespace BackPredictFinance.Services.AdminGovernance
{
    public interface IWordingPublicationService
    {
        /// <summary>
        /// Retourne l'etat de publication actif pour la gouvernance de wording.
        /// </summary>
        Task<WordingPublicationState?> GetActivePublicationAsync(CancellationToken ct = default);

        /// <summary>
        /// Resolve le template de wording actif pour un etat de recommandation backend ferme.
        /// </summary>
        Task<WordingScenarioTemplate?> ResolveScenarioAsync(RecommendationKind recommendationKind, HoldingStatusEnum holdingStatus, CancellationToken ct = default);
    }

    public sealed class WordingPublicationService : BaseService, IWordingPublicationService
    {
        public WordingPublicationService(IServiceProvider serviceProvider) : base(serviceProvider)
        {
        }

        public async Task<WordingPublicationState?> GetActivePublicationAsync(CancellationToken ct = default)
        {
            return await _financeDbContext.RecommendationWordingVersions
                .AsNoTracking()
                .Where(x => x.IsActive)
                .OrderByDescending(x => x.ActivatedAtUtc)
                .ThenBy(x => x.WordingVersionId)
                .Select(x => new WordingPublicationState
                {
                    IsActive = x.IsActive,
                    ActivatedAtUtc = x.ActivatedAtUtc,
                    RecommendationPolicyVersion = x.RecommendationPolicyVersion,
                    ExplanationPolicyVersion = x.ExplanationPolicyVersion,
                    AffectedDomains = SplitCsv(x.AffectedDomains)
                })
                .FirstOrDefaultAsync(ct);
        }

        public async Task<WordingScenarioTemplate?> ResolveScenarioAsync(RecommendationKind recommendationKind, HoldingStatusEnum holdingStatus, CancellationToken ct = default)
        {
            var scenario = await _financeDbContext.RecommendationWordingScenarios
                .AsNoTracking()
                .Where(x => x.WordingVersion.IsActive
                    && x.RecommendationKind == recommendationKind
                    && x.HoldingStatus == holdingStatus)
                .OrderByDescending(x => x.WordingVersion.ActivatedAtUtc)
                .ThenBy(x => x.ScenarioCode)
                .FirstOrDefaultAsync(ct);

            return scenario is null ? null : MapScenario(scenario);
        }

        internal static WordingScenarioTemplate MapScenario(RecommendationWordingScenario scenario)
        {
            return new WordingScenarioTemplate
            {
                ScenarioCode = scenario.ScenarioCode,
                RecommendationKind = scenario.RecommendationKind,
                HoldingStatus = scenario.HoldingStatus,
                ActionVerbFamilyCode = scenario.ActionVerbFamilyCode,
                SupportedStrengths = SplitStrengthFamily(scenario.RecommendationStrengthFamily),
                TemplateSummary = scenario.TemplateSummary
            };
        }

        internal static List<string> SplitCsv(string csv)
        {
            return string.IsNullOrWhiteSpace(csv)
                ? []
                : [.. csv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)];
        }

        internal static List<RecommendationStrengthEnum> SplitStrengthFamily(string csv)
        {
            if (string.IsNullOrWhiteSpace(csv))
            {
                return [];
            }

            return [.. csv
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(value => Enum.Parse<RecommendationStrengthEnum>(value, ignoreCase: true))];
        }
    }
}
