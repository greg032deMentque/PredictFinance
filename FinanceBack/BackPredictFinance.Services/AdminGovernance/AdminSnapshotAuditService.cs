using System.Text.Json;
using BackPredictFinance.Common.AnalysisV1;
using BackPredictFinance.Common.enums;
using BackPredictFinance.ViewModels.AdminViewModels.SnapshotAudit;
using Microsoft.EntityFrameworkCore;

namespace BackPredictFinance.Services.AdminGovernance
{
    public interface IAdminSnapshotAuditService
    {
        Task<List<SnapshotAuditItemViewModel>> GetRecentAsync(int take, CancellationToken ct = default);
        Task<SnapshotAuditDetailViewModel> GetDetailAsync(string analysisRunId, CancellationToken ct = default);
        Task<SnapshotAuditComparisonViewModel> CompareAsync(string leftAnalysisRunId, string rightAnalysisRunId, CancellationToken ct = default);
    }

    public sealed class AdminSnapshotAuditService : BaseService, IAdminSnapshotAuditService
    {
        public AdminSnapshotAuditService(IServiceProvider serviceProvider) : base(serviceProvider)
        {
        }

        public async Task<List<SnapshotAuditItemViewModel>> GetRecentAsync(int take, CancellationToken ct = default)
        {
            var normalizedTake = Math.Clamp(take, 1, 200);
            var analysisRuns = await _financeDbContext.AnalysisRuns
                .AsNoTracking()
                .Include(x => x.Asset)
                .Include(x => x.ModelSnapshot)
                .OrderByDescending(x => x.CompletedAtUtc ?? x.StartedAtUtc)
                .Take(normalizedTake)
                .ToListAsync(ct);

            return analysisRuns.Select(MapAuditItem).ToList();
        }

        public async Task<SnapshotAuditDetailViewModel> GetDetailAsync(string analysisRunId, CancellationToken ct = default)
        {
            var normalizedAnalysisRunId = (analysisRunId ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(normalizedAnalysisRunId))
            {
                throw new ArgumentException("Analysis run id is required.", nameof(analysisRunId));
            }

            var analysisRun = await _financeDbContext.AnalysisRuns
                .AsNoTracking()
                .Include(x => x.Asset)
                .Include(x => x.ModelSnapshot)
                .Include(x => x.DecisionSignal)
                .FirstOrDefaultAsync(x => x.Id == normalizedAnalysisRunId, ct);

            if (analysisRun is null)
            {
                throw new KeyNotFoundException($"Analysis run '{normalizedAnalysisRunId}' was not found.");
            }

            var parsedPayload = ParsePayload(analysisRun.RawPayload);
            return new SnapshotAuditDetailViewModel
            {
                AnalysisRunId = analysisRun.Id,
                UserId = analysisRun.UserId,
                AssetId = analysisRun.AssetId,
                Symbol = analysisRun.Asset?.Symbol ?? string.Empty,
                Status = analysisRun.Status.ToString(),
                StartedAtUtc = analysisRun.StartedAtUtc,
                CompletedAtUtc = analysisRun.CompletedAtUtc,
                ErrorMessage = analysisRun.ErrorMessage,
                RawPayload = analysisRun.RawPayload,
                TraceId = parsedPayload.TraceId,
                RequestedPatternIds = parsedPayload.RequestedPatternIds,
                ExecutedPatternIds = parsedPayload.ExecutedPatternIds,
                PrimaryPatternId = parsedPayload.PrimaryPatternId,
                RecommendationAction = parsedPayload.RecommendationAction,
                RecommendationPolicyVersion = parsedPayload.RecommendationPolicyVersion,
                ExplanationPolicyVersion = parsedPayload.ExplanationPolicyVersion,
                AnalysisEngineVersion = parsedPayload.AnalysisEngineVersion,
                ModelStatus = analysisRun.ModelSnapshot?.ModelStatus,
                ModelMessage = analysisRun.ModelSnapshot?.ModelMessage,
                DecisionAction = analysisRun.DecisionSignal?.Action.ToString(),
                DecisionSummary = analysisRun.DecisionSignal?.Reason
            };
        }

        public async Task<SnapshotAuditComparisonViewModel> CompareAsync(string leftAnalysisRunId, string rightAnalysisRunId, CancellationToken ct = default)
        {
            var left = await GetDetailAsync(leftAnalysisRunId, ct);
            var right = await GetDetailAsync(rightAnalysisRunId, ct);

            return new SnapshotAuditComparisonViewModel
            {
                LeftAnalysisRunId = left.AnalysisRunId,
                RightAnalysisRunId = right.AnalysisRunId,
                SameUser = string.Equals(left.UserId, right.UserId, StringComparison.Ordinal),
                SameAsset = string.Equals(left.AssetId, right.AssetId, StringComparison.Ordinal),
                SamePrimaryPattern = string.Equals(left.PrimaryPatternId, right.PrimaryPatternId, StringComparison.Ordinal),
                SameRecommendationAction = string.Equals(left.RecommendationAction, right.RecommendationAction, StringComparison.Ordinal),
                LeftPrimaryPatternId = left.PrimaryPatternId,
                RightPrimaryPatternId = right.PrimaryPatternId,
                LeftRecommendationAction = left.RecommendationAction,
                RightRecommendationAction = right.RecommendationAction,
                LeftAnalysisEngineVersion = left.AnalysisEngineVersion,
                RightAnalysisEngineVersion = right.AnalysisEngineVersion,
                LeftModelStatus = left.ModelStatus,
                RightModelStatus = right.ModelStatus
            };
        }

        private static SnapshotAuditItemViewModel MapAuditItem(Datas.Entities.AnalysisRun analysisRun)
        {
            var parsedPayload = ParsePayload(analysisRun.RawPayload);
            return new SnapshotAuditItemViewModel
            {
                AnalysisRunId = analysisRun.Id,
                UserId = analysisRun.UserId,
                AssetId = analysisRun.AssetId,
                Symbol = analysisRun.Asset?.Symbol ?? string.Empty,
                Status = analysisRun.Status.ToString(),
                StartedAtUtc = analysisRun.StartedAtUtc,
                CompletedAtUtc = analysisRun.CompletedAtUtc,
                TraceId = parsedPayload.TraceId,
                PrimaryPatternId = parsedPayload.PrimaryPatternId,
                ExecutedPatternIds = parsedPayload.ExecutedPatternIds,
                RecommendationAction = parsedPayload.RecommendationAction,
                ModelStatus = analysisRun.ModelSnapshot?.ModelStatus,
                AnalysisEngineVersion = parsedPayload.AnalysisEngineVersion
            };
        }

        // Le RawPayload est écrit avec AnalysisSnapshotJsonOptions.Shared (camelCase, voir
        // AnalysisSnapshotPersistenceService.TryPersistAnalysisRunAsync) : la lecture doit utiliser les
        // mêmes options (case-insensitive) plutôt qu'un JsonDocument.TryGetProperty sensible à la casse,
        // qui rate silencieusement toutes les propriétés (piège déjà documenté ailleurs pour ce payload).
        private static ParsedSnapshotPayload ParsePayload(string? rawPayload)
        {
            if (string.IsNullOrWhiteSpace(rawPayload))
            {
                return new ParsedSnapshotPayload();
            }

            try
            {
                var payload = JsonSerializer.Deserialize<RawSnapshotPayload>(rawPayload, AnalysisSnapshotJsonOptions.Shared);
                if (payload is null)
                {
                    return new ParsedSnapshotPayload();
                }

                return new ParsedSnapshotPayload
                {
                    TraceId = payload.TraceId ?? string.Empty,
                    PrimaryPatternId = payload.PrimaryPatternId,
                    RecommendationAction = payload.Recommendation?.RecommendationPayload?.Kind,
                    RecommendationPolicyVersion = payload.RecommendationPolicyVersion,
                    ExplanationPolicyVersion = payload.ExplanationPolicyVersion,
                    AnalysisEngineVersion = payload.AnalysisEngineVersion,
                    RequestedPatternIds = payload.RequestedPatternIds ?? [],
                    ExecutedPatternIds = payload.ExecutedPatternIds ?? []
                };
            }
            catch (JsonException)
            {
                return new ParsedSnapshotPayload();
            }
        }

        private sealed class ParsedSnapshotPayload
        {
            public string TraceId { get; set; } = string.Empty;
            public string? PrimaryPatternId { get; set; }
            public string? RecommendationAction { get; set; }
            public string? RecommendationPolicyVersion { get; set; }
            public string? ExplanationPolicyVersion { get; set; }
            public string? AnalysisEngineVersion { get; set; }
            public List<string> RequestedPatternIds { get; set; } = [];
            public List<string> ExecutedPatternIds { get; set; } = [];
        }

        // Sous-ensemble du payload réel (PersistedAnalysisSnapshotPayload, privé à
        // AnalysisSnapshotPersistenceService) nécessaire à l'audit : seules les propriétés lues ici
        // doivent matcher le JSON, les autres sont ignorées par la désérialisation.
        private sealed class RawSnapshotPayload
        {
            public string? TraceId { get; set; }
            public string? PrimaryPatternId { get; set; }
            public string? RecommendationPolicyVersion { get; set; }
            public string? ExplanationPolicyVersion { get; set; }
            public string? AnalysisEngineVersion { get; set; }
            public List<string>? RequestedPatternIds { get; set; }
            public List<string>? ExecutedPatternIds { get; set; }
            public RawSnapshotRecommendation? Recommendation { get; set; }
        }

        private sealed class RawSnapshotRecommendation
        {
            public RawSnapshotRecommendationPayload? RecommendationPayload { get; set; }
        }

        private sealed class RawSnapshotRecommendationPayload
        {
            public string? Kind { get; set; }
        }
    }
}
