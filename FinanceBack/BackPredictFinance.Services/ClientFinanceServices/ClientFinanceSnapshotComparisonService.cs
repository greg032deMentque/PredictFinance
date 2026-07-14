using BackPredictFinance.Common.AnalysisV1;
using BackPredictFinance.ViewModels.ClientFinanceViewModels.Snapshots;
using Microsoft.EntityFrameworkCore;

namespace BackPredictFinance.Services.ClientFinanceServices
{
    /// <summary>
    /// Expose la comparaison de deux snapshots d'analyse persistés.
    /// </summary>
    public interface IClientFinanceSnapshotComparisonService
    {
        /// <summary>
        /// Compare deux snapshots d'analyse et retourne leur projection comparative.
        /// </summary>
        Task<SnapshotComparisonViewModel?> CompareAsync(SnapshotComparisonRequestViewModel request, CancellationToken ct = default);
    }

    /// <summary>
    /// Implémente la lecture comparative de snapshots historisés.
    /// </summary>
    public sealed class ClientFinanceSnapshotComparisonService : BaseService, IClientFinanceSnapshotComparisonService
    {
        private readonly IClientFinanceAssetSupportService _assetSupportService;
        private readonly IClientFinanceProjectionService _projectionService;

        public ClientFinanceSnapshotComparisonService(
            IServiceProvider serviceProvider,
            IClientFinanceAssetSupportService assetSupportService,
            IClientFinanceProjectionService projectionService)
            : base(serviceProvider)
        {
            _assetSupportService = assetSupportService;
            _projectionService = projectionService;
        }

        public async Task<SnapshotComparisonViewModel?> CompareAsync(SnapshotComparisonRequestViewModel request, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(request);

            if (string.IsNullOrWhiteSpace(request.LeftSnapshotId))
            {
                throw new ArgumentException("Le snapshot de gauche est obligatoire.", nameof(request.LeftSnapshotId));
            }

            if (string.IsNullOrWhiteSpace(request.RightSnapshotId))
            {
                throw new ArgumentException("Le snapshot de droite est obligatoire.", nameof(request.RightSnapshotId));
            }

            var requestedIds = new[]
            {
                request.LeftSnapshotId.Trim(),
                request.RightSnapshotId.Trim()
            };

            var analysisRuns = await _financeDbContext.AnalysisRuns
                .AsNoTracking()
                .Include(x => x.Asset)
                .Where(x => x.UserId == _assetSupportService.GetRequiredCurrentUserId()
                    && x.Status == _projectionService.CompletedStatus
                    && requestedIds.Contains(x.Id))
                .ToListAsync(ct);
            if (analysisRuns.Count != 2)
            {
                return null;
            }

            var leftRun = analysisRuns.FirstOrDefault(x => x.Id == request.LeftSnapshotId.Trim());
            var rightRun = analysisRuns.FirstOrDefault(x => x.Id == request.RightSnapshotId.Trim());
            if (leftRun == null || rightRun == null)
            {
                return null;
            }

            var leftSnapshot = _projectionService.TryReadSnapshot(leftRun.RawPayload);
            var rightSnapshot = _projectionService.TryReadSnapshot(rightRun.RawPayload);
            if (leftSnapshot == null || rightSnapshot == null)
            {
                return null;
            }

            var peaStatuses = await _financeDbContext.AssetPeaEligibilities
                .AsNoTracking()
                .Where(x => x.AssetId == leftRun.AssetId || x.AssetId == rightRun.AssetId)
                .GroupBy(x => x.AssetId)
                .Select(x => x.OrderByDescending(y => y.CheckedUtc ?? y.CreatedAtUtc).First())
                .ToDictionaryAsync(x => x.AssetId, x => x.EligibilityStatus, ct);

            peaStatuses.TryGetValue(leftRun.AssetId, out var leftPeaStatus);
            peaStatuses.TryGetValue(rightRun.AssetId, out var rightPeaStatus);

            var left = _projectionService.BuildHistoryItem(leftRun.Asset, leftRun.Id, leftSnapshot, leftPeaStatus);
            var right = _projectionService.BuildHistoryItem(rightRun.Asset, rightRun.Id, rightSnapshot, rightPeaStatus);

            var nonComparabilityReasons = new List<string>();
            if (!string.Equals(leftRun.AssetId, rightRun.AssetId, StringComparison.Ordinal))
            {
                nonComparabilityReasons.Add("Les snapshots demandes n appartiennent pas au meme instrument.");
            }

            if (!string.Equals(leftSnapshot.AnalysisEngineVersion, rightSnapshot.AnalysisEngineVersion, StringComparison.Ordinal))
            {
                nonComparabilityReasons.Add("La version du moteur d analyse differe entre les deux snapshots.");
            }

            if (!string.Equals(leftSnapshot.RecommendationPolicyVersion, rightSnapshot.RecommendationPolicyVersion, StringComparison.Ordinal))
            {
                nonComparabilityReasons.Add("La version de la politique de recommandation differe entre les deux snapshots.");
            }

            nonComparabilityReasons.Add("La lecture support n est pas persistee dans les snapshots V1 actuels et ne peut pas etre comparee de maniere fiable.");

            return new SnapshotComparisonViewModel
            {
                Left = left,
                Right = right,
                MarketChanges = BuildMarketChanges(leftSnapshot, rightSnapshot),
                SupportChanges =
                [
                    new SnapshotDeltaItemViewModel
                    {
                        FieldCode = "support_snapshot_availability",
                        DisplayLabel = "Lecture support",
                        LeftValue = "Non persistee",
                        RightValue = "Non persistee",
                        ChangeKind = "limited",
                        EvidenceType = "source_fact"
                    }
                ],
                RecommendationChanges = BuildRecommendationChanges(leftSnapshot, rightSnapshot),
                NonComparabilityReasons = nonComparabilityReasons
            };
        }

        private static List<SnapshotDeltaItemViewModel> BuildMarketChanges(
            PersistedAnalysisSnapshotPayloadReadModel leftSnapshot,
            PersistedAnalysisSnapshotPayloadReadModel rightSnapshot)
        {
            var leftPrimary = leftSnapshot.PrimaryPattern;
            var rightPrimary = rightSnapshot.PrimaryPattern;

            return BuildDeltaItems(
                ("outcome", "Issue de l analyse", leftSnapshot.Outcome.ToString(), rightSnapshot.Outcome.ToString(), "source_fact"),
                ("primary_pattern", "Pattern principal", leftPrimary?.DisplayName ?? leftPrimary?.PatternId, rightPrimary?.DisplayName ?? rightPrimary?.PatternId, "source_fact"),
                ("phase", "Phase", leftPrimary?.Detection.CurrentPhaseLabel ?? leftPrimary?.Detection.CurrentPhaseCode, rightPrimary?.Detection.CurrentPhaseLabel ?? rightPrimary?.Detection.CurrentPhaseCode, "source_fact"),
                ("confidence_label", "Confiance", leftPrimary?.Scoring.ConfidenceLabel, rightPrimary?.Scoring.ConfidenceLabel, "derived_consequence"),
                ("validation_state", "Validation", leftPrimary?.Validation.State, rightPrimary?.Validation.State, "source_fact"),
                ("invalidation_level", "Niveau d invalidation", FormatDecimal(leftPrimary?.Invalidation.InvalidationLevel), FormatDecimal(rightPrimary?.Invalidation.InvalidationLevel), "source_fact"),
                ("risk_hint", "Indice risque / rendement", leftPrimary?.RiskHints.PositioningNote, rightPrimary?.RiskHints.PositioningNote, "derived_consequence")
            );
        }

        private static List<SnapshotDeltaItemViewModel> BuildRecommendationChanges(
            PersistedAnalysisSnapshotPayloadReadModel leftSnapshot,
            PersistedAnalysisSnapshotPayloadReadModel rightSnapshot)
        {
            var leftRecommendation = leftSnapshot.Recommendation?.RecommendationPayload;
            var rightRecommendation = rightSnapshot.Recommendation?.RecommendationPayload;

            return BuildDeltaItems(
                ("recommendation_kind", "Action recommandee", leftRecommendation?.Kind.ToString(), rightRecommendation?.Kind.ToString(), "derived_consequence"),
                ("recommendation_rationale", "Justification", leftRecommendation?.Rationale, rightRecommendation?.Rationale, "derived_consequence"),
                ("review_horizon", "Horizon de revue", FormatInt(leftRecommendation?.ReviewHorizonDays), FormatInt(rightRecommendation?.ReviewHorizonDays), "derived_consequence"),
                ("recommendation_policy_version", "Version de politique", leftSnapshot.RecommendationPolicyVersion, rightSnapshot.RecommendationPolicyVersion, "source_fact")
            );
        }

        private static List<SnapshotDeltaItemViewModel> BuildDeltaItems(params (string FieldCode, string DisplayLabel, string? LeftValue, string? RightValue, string EvidenceType)[] candidates)
        {
            var items = new List<SnapshotDeltaItemViewModel>();
            foreach (var candidate in candidates)
            {
                var leftValue = Normalize(candidate.LeftValue);
                var rightValue = Normalize(candidate.RightValue);
                if (leftValue == rightValue)
                {
                    continue;
                }

                items.Add(new SnapshotDeltaItemViewModel
                {
                    FieldCode = candidate.FieldCode,
                    DisplayLabel = candidate.DisplayLabel,
                    LeftValue = leftValue,
                    RightValue = rightValue,
                    ChangeKind = "changed",
                    EvidenceType = candidate.EvidenceType
                });
            }

            return items;
        }

        private static string Normalize(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? "Non renseigne" : value.Trim();
        }

        private static string? FormatDecimal(decimal? value)
        {
            return value.HasValue ? value.Value.ToString("0.####") : null;
        }

        private static string? FormatInt(int? value)
        {
            return value.HasValue ? value.Value.ToString() : null;
        }
    }
}
