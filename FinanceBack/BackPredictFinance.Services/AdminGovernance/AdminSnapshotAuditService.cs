using System.Text.Json;
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

        private static ParsedSnapshotPayload ParsePayload(string? rawPayload)
        {
            if (string.IsNullOrWhiteSpace(rawPayload))
            {
                return new ParsedSnapshotPayload();
            }

            try
            {
                using var document = JsonDocument.Parse(rawPayload);
                var root = document.RootElement;
                return new ParsedSnapshotPayload
                {
                    TraceId = ReadString(root, "TraceId") ?? string.Empty,
                    PrimaryPatternId = ReadString(root, "PrimaryPatternId"),
                    RecommendationAction = ReadNestedString(root, "Recommendation", "Action"),
                    RecommendationPolicyVersion = ReadString(root, "RecommendationPolicyVersion"),
                    ExplanationPolicyVersion = ReadString(root, "ExplanationPolicyVersion"),
                    AnalysisEngineVersion = ReadString(root, "AnalysisEngineVersion"),
                    RequestedPatternIds = ReadStringArray(root, "RequestedPatternIds"),
                    ExecutedPatternIds = ReadStringArray(root, "ExecutedPatternIds")
                };
            }
            catch (JsonException)
            {
                return new ParsedSnapshotPayload();
            }
        }

        private static string? ReadString(JsonElement root, string propertyName)
        {
            return root.TryGetProperty(propertyName, out var value) && value.ValueKind == JsonValueKind.String
                ? value.GetString()
                : null;
        }

        private static string? ReadNestedString(JsonElement root, string parentPropertyName, string childPropertyName)
        {
            if (!root.TryGetProperty(parentPropertyName, out var parent) || parent.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            return parent.TryGetProperty(childPropertyName, out var child) && child.ValueKind == JsonValueKind.String
                ? child.GetString()
                : null;
        }

        private static List<string> ReadStringArray(JsonElement root, string propertyName)
        {
            if (!root.TryGetProperty(propertyName, out var array) || array.ValueKind != JsonValueKind.Array)
            {
                return [];
            }

            return array
                .EnumerateArray()
                .Where(x => x.ValueKind == JsonValueKind.String)
                .Select(x => x.GetString())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Cast<string>()
                .ToList();
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
    }
}
