using System.Text.Json;
using BackPredictFinance.Common.AnalysisV1;
using BackPredictFinance.Common.enums;
using static BackPredictFinance.Common.AnalysisV1.ConfidenceThresholds;
using BackPredictFinance.Services;
using BackPredictFinance.ViewModels.ClientFinanceViewModels.Analysis;
using Microsoft.EntityFrameworkCore;

namespace BackPredictFinance.Services.ClientFinanceServices.Analysis
{
    /// <summary>
    /// Projette les résultats d'analyse backend vers les modèles utilisés par ClientFinance.
    /// </summary>
    public interface IAnalysisResultProjectionService
    {
        /// <summary>
        /// Projette le résultat immédiat d'une exécution d'analyse.
        /// </summary>
        AnalysisResultViewModel MapRunResult(AnalysisResponseViewModel response);
        /// <summary>
        /// Retourne les analyses récentes projetées pour un utilisateur.
        /// </summary>
        Task<List<AnalysisResultViewModel>> GetRecentAnalysesAsync(string userId, int take, CancellationToken ct = default);
    }

    /// <summary>
    /// Implémente la projection des résultats et snapshots d'analyse.
    /// </summary>
    public sealed class AnalysisResultProjectionService : BaseService, IAnalysisResultProjectionService
    {
        private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

        public AnalysisResultProjectionService(IServiceProvider serviceProvider)
            : base(serviceProvider)
        {
        }

        public AnalysisResultViewModel MapRunResult(AnalysisResponseViewModel response)
        {
            ArgumentNullException.ThrowIfNull(response);

            return BuildProjectedResult(
                response.AnalysisId,
                response.Instrument.Symbol,
                response.Instrument.DisplayName,
                response.MainPattern,
                response.Recommendation,
                response.PedagogicalSummary,
                response.GeneratedAtUtc,
                response.ModelStatus,
                string.IsNullOrWhiteSpace(response.ModelMessage) ? string.Join(" ", response.Warnings) : response.ModelMessage);
        }

        public async Task<List<AnalysisResultViewModel>> GetRecentAnalysesAsync(string userId, int take, CancellationToken ct = default)
        {
            var size = Math.Clamp(take, 1, 100);

            var analysisRuns = await _financeDbContext.AnalysisRuns
                .AsNoTracking()
                .Where(run => run.UserId == userId && run.Status == AnalysisRunStatusEnum.Completed)
                .OrderByDescending(run => run.CompletedAtUtc ?? run.StartedAtUtc)
                .Take(size)
                .ToListAsync(ct);

            return analysisRuns
                .Select(TryMapAnalysisRunResult)
                .Where(result => result != null)
                .Select(result => result!)
                .ToList();
        }

        private static AnalysisResultViewModel? TryMapAnalysisRunResult(BackPredictFinance.Datas.Entities.AnalysisRun analysisRun)
        {
            if (string.IsNullOrWhiteSpace(analysisRun.RawPayload))
            {
                return null;
            }

            try
            {
                var snapshot = JsonSerializer.Deserialize<StoredAnalysisSnapshot>(analysisRun.RawPayload, SerializerOptions);
                if (!CanProjectSnapshot(snapshot))
                {
                    return null;
                }

                var primaryPattern = snapshot.PatternRows
                    .OrderByDescending(pattern => pattern.IsPrimaryDisplayCandidate)
                    .ThenBy(pattern => pattern.DisplayRank)
                    .Select(pattern => pattern.PatternAssessmentPayload)
                    .FirstOrDefault();

                return BuildProjectedResult(
                    analysisRun.Id,
                    snapshot.InstrumentSnapshot.Symbol,
                    snapshot.InstrumentSnapshot.DisplayName,
                    primaryPattern,
                    snapshot.Recommendation?.RecommendationPayload,
                    snapshot.PedagogicalSummary,
                    snapshot.CompletedAtUtc,
                    snapshot.ModelSnapshot.ModelStatus,
                    snapshot.ModelSnapshot.ModelMessage);
            }
            catch (JsonException)
            {
                return null;
            }
            catch (InvalidOperationException)
            {
                return null;
            }
        }

        private static bool CanProjectSnapshot(StoredAnalysisSnapshot? snapshot)
        {
            if (snapshot is null)
            {
                return false;
            }

            if (snapshot.InstrumentSnapshot is null)
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(snapshot.InstrumentSnapshot.Symbol))
            {
                return false;
            }

            if (snapshot.CompletedAtUtc == default)
            {
                return false;
            }

            if (snapshot.ModelSnapshot is null)
            {
                return false;
            }

            return snapshot.PatternRows.Any(pattern =>
                pattern?.PatternAssessmentPayload is not null &&
                !string.IsNullOrWhiteSpace(pattern.PatternAssessmentPayload.PatternId));
        }

        private static AnalysisResultViewModel BuildProjectedResult(
            string analysisId,
            string symbol,
            string companyName,
            PatternAssessmentContract? primaryPattern,
            AnalysisRecommendation? recommendation,
            string fallbackReason,
            DateTime generatedAtUtc,
            ModelStatusEnum modelStatus,
            string modelMessage)
        {
            if (primaryPattern is null)
            {
                throw new InvalidOperationException("A projected analysis result requires a primary pattern.");
            }

            var confidence = primaryPattern.Scoring.ConfidenceScore;
            var recommendationAction = MapRecommendationKind(recommendation?.Kind);
            var isActionable = recommendationAction is RecommendationActionEnum.Buy or RecommendationActionEnum.Sell;

            return new AnalysisResultViewModel
            {
                Id = analysisId,
                Symbol = symbol,
                CompanyName = companyName,
                Pattern = primaryPattern.PatternId,
                Phase = primaryPattern.Detection.CurrentPhaseCode ?? string.Empty,
                Probability = confidence,
                RecommendationAction = recommendationAction,
                RecommendationReason = recommendation?.Rationale ?? fallbackReason,
                RiskLevel = InferRiskLevel(confidence, isActionable),
                RecommendationHorizonDays = recommendation?.ReviewHorizonDays ?? 0,
                PredictedAt = generatedAtUtc,
                IsActionable = isActionable,
                ModelStatus = modelStatus,
                ModelMessage = modelMessage,
                CurrentPrice = primaryPattern.Detection.CurrentPrice,
                NecklinePrice = null,
                TargetPrice = primaryPattern.RiskHints.SuggestedTakeProfit,
                InvalidationPrice = primaryPattern.Invalidation.InvalidationLevel
            };
        }

        private static RecommendationActionEnum MapRecommendationKind(RecommendationKind? kind)
        {
            return kind switch
            {
                RecommendationKind.Buy => RecommendationActionEnum.Buy,
                RecommendationKind.Reinforce => RecommendationActionEnum.Buy,
                RecommendationKind.Lighten => RecommendationActionEnum.Sell,
                RecommendationKind.Sell => RecommendationActionEnum.Sell,
                _ => RecommendationActionEnum.Hold
            };
        }

        private static RiskLevelEnum InferRiskLevel(decimal confidence, bool actionable)
        {
            if (!actionable)
            {
                return RiskLevelEnum.Information;
            }

            if (confidence >= HighFloor)
            {
                return RiskLevelEnum.Low;
            }

            if (confidence >= MediumFloor)
            {
                return RiskLevelEnum.Moderate;
            }

            return RiskLevelEnum.High;
        }

        private sealed class StoredAnalysisSnapshot
        {
            public Instrument InstrumentSnapshot { get; set; } = new();
            public DateTime CompletedAtUtc { get; set; }
            public string PedagogicalSummary { get; set; } = string.Empty;
            public List<AnalysisSnapshotPatternRow> PatternRows { get; set; } = [];
            public AnalysisSnapshotRecommendation? Recommendation { get; set; }
            public StoredModelSnapshot ModelSnapshot { get; set; } = new();
        }

        private sealed class StoredModelSnapshot
        {
            public ModelStatusEnum ModelStatus { get; set; } = ModelStatusEnum.NoGo;
            public string ModelMessage { get; set; } = string.Empty;
        }
    }
}
