using System.Text.Json;
using BackPredictFinance.Common.AnalysisV1;
using BackPredictFinance.Common.enums;
using BackPredictFinance.Common.MarketData;
using static BackPredictFinance.Common.AnalysisV1.ConfidenceThresholds;
using BackPredictFinance.Services;
using BackPredictFinance.ViewModels.ClientFinanceViewModels.Analysis;
using BackPredictFinance.Patterns.Common;
using Microsoft.EntityFrameworkCore;

namespace BackPredictFinance.Services.ClientFinanceServices.Analysis
{
    public interface IAnalysisResultProjectionService
    {
        AnalysisResultViewModel MapRunResult(AnalysisResponseViewModel response);
        AnalysisDossierViewModel MapDossier(AnalysisResponseViewModel response);
        Task<List<AnalysisResultViewModel>> GetRecentAnalysesAsync(string userId, int take, CancellationToken ct = default);
    }

    public sealed class AnalysisResultProjectionService : BaseService, IAnalysisResultProjectionService
    {
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

        public AnalysisDossierViewModel MapDossier(AnalysisResponseViewModel response)
        {
            ArgumentNullException.ThrowIfNull(response);

            var mainPatternVm = response.MainPattern is not null
                ? MapPatternToViewModel(response.MainPattern, response.Recommendation, response.RiskContext)
                : null;

            var alternativeVms = response.AlternativePatterns
                .Select(p => MapPatternToViewModel(p, response.Recommendation, response.RiskContext))
                .ToList();

            var analysisWindow = ResolveAnalysisWindow(response.MainPattern, response.AlternativePatterns);

            var priceSeries = response.Candles
                .Select(c => new CandleViewModel
                {
                    Timestamp = c.Date,
                    Open = c.Open,
                    High = c.High,
                    Low = c.Low,
                    Close = c.Close,
                    Volume = c.Volume
                })
                .ToList();

            var srZones = SupportResistanceDetector.Detect(response.Candles)
                .Select(z => new SupportResistanceZoneViewModel
                {
                    PriceLow = z.PriceLow,
                    PriceHigh = z.PriceHigh,
                    PriceMid = z.PriceMid,
                    TouchCount = z.TouchCount,
                    ZoneType = z.ZoneType,
                    Strength = z.Strength
                })
                .ToList();

            return new AnalysisDossierViewModel
            {
                Id = response.AnalysisId,
                Symbol = response.Instrument.Symbol,
                CompanyName = response.Instrument.DisplayName,
                Outcome = response.Outcome.ToString(),
                OutcomeMessage = BuildOutcomeMessage(response),
                GlobalSummary = response.PedagogicalSummary,
                PredictedAt = response.GeneratedAtUtc,
                ModelStatus = response.ModelStatus.ToString(),
                ModelMessage = response.ModelMessage,
                AnalysisWindow = analysisWindow,
                PriceSeries = priceSeries,
                MainPattern = mainPatternVm,
                AlternativePatterns = alternativeVms,
                SrZones = srZones,
                RiskContext = response.RiskContext,
                TechnicalContext = response.TechnicalContext
            };
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
                // Piège connu : ces options DOIVENT être celles utilisées à l'écriture
                // (AnalysisSnapshotPersistenceService, AnalysisSnapshotJsonOptions.Shared, avec
                // JsonStringEnumConverter). Un ancien historique a été écrit avant l'unification des
                // options de sérialisation, quand la persistance sérialisait les enums en chaînes
                // (Outcome, ModelStatus...) sans que la projection ne le sache : la désérialisation
                // levait alors une JsonException, silencieusement absorbée ci-dessous (le snapshot est
                // simplement ignoré). Si RawPayload recommence à diverger entre écriture et lecture,
                // ce comportement resurgit sans erreur visible côté appelant.
                var snapshot = JsonSerializer.Deserialize<StoredAnalysisSnapshot>(analysisRun.RawPayload, AnalysisSnapshotJsonOptions.Shared);
                if (!CanProjectSnapshot(snapshot))
                {
                    return null;
                }

                // Reprend l'ordre figé à la persistance (DisplayRank) : priorité au candidat marqué
                // primaire à l'affichage, sinon le premier du classement enregistré.
                var primaryPattern = snapshot!.PatternRows
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
                // Snapshot illisible avec les options courantes (ex. historique pré-unification des
                // JsonSerializerOptions, ou payload corrompu) : on l'exclut silencieusement de la liste
                // plutôt que de faire échouer tout l'historique de l'utilisateur pour une seule ligne.
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

        private static AnalysisPatternViewModel MapPatternToViewModel(PatternAssessmentContract pattern, AnalysisRecommendation? recommendation, AnalysisRiskContext? riskContext = null)
        {
            var recommendationAction = MapRecommendationKind(recommendation?.Kind);
            var isActionable = recommendationAction is RecommendationActionEnum.Buy or RecommendationActionEnum.Sell;
            var riskLevel = InferRiskLevel(pattern.Scoring.ConfidenceScore, isActionable);

            return new AnalysisPatternViewModel
            {
                PatternId = pattern.PatternId,
                DisplayName = pattern.DisplayName,
                PedagogicalDescription = pattern.PedagogicalDescription,
                PhaseCode = pattern.Detection.CurrentPhaseCode,
                PhaseLabel = pattern.Detection.CurrentPhaseLabel,
                Status = pattern.Detection.Status.ToString(),
                IsCompatible = pattern.Detection.IsCompatible,
                ConfidenceScore = pattern.Scoring.ConfidenceScore,
                ConfidenceLabel = pattern.Scoring.ConfidenceLabel,
                ProbabilityScore = pattern.Scoring.ProbabilityScore,
                ProbabilityLabel = pattern.Scoring.ProbabilityLabel,
                IsCredible = pattern.Scoring.IsCredible,
                ScoreReasons = pattern.Scoring.ScoreReasons.ToList(),
                CurrentPrice = pattern.Detection.CurrentPrice,
                NecklinePrice = null,
                ValidationState = pattern.Validation.State,
                ValidationLevel = pattern.Validation.ValidatedAtPrice,
                ValidationDate = pattern.Validation.ValidatedAtDate,
                InvalidationState = pattern.Invalidation.State,
                InvalidationLevel = pattern.Invalidation.InvalidationLevel,
                InvalidationDate = pattern.Invalidation.BreachedAtDate,
                HasRiskPlan = pattern.RiskHints.HasRiskPlan,
                SuggestedStopLoss = pattern.RiskHints.SuggestedStopLoss,
                SuggestedTakeProfit = pattern.RiskHints.SuggestedTakeProfit,
                RiskRewardRatio = pattern.RiskHints.RiskRewardRatio,
                PositioningNote = pattern.RiskHints.PositioningNote,
                StructuralPoints = pattern.Detection.StructuralPoints
                    .Select(sp => new StructuralPointViewModel
                    {
                        PointType = sp.PointType,
                        Timestamp = sp.Timestamp,
                        Price = sp.Price
                    })
                    .ToList(),
                WhyListed = pattern.Explanation.WhyListed,
                PedagogicalSummary = pattern.Explanation.PedagogicalSummary,
                AmbiguityNote = pattern.Explanation.AmbiguityNote,
                LimitationsNote = pattern.Explanation.LimitationsNote,
                IsActionable = isActionable,
                RecommendationAction = recommendationAction.ToString(),
                RecommendationReason = recommendation?.Rationale ?? string.Empty,
                RiskLevel = riskLevel.ToString(),
                RecommendationHorizonDays = recommendation?.ReviewHorizonDays ?? 0,
                AtrStopLossPrice = pattern.RiskHints.AtrStopLossPrice,
                AtrTarget1Price = pattern.RiskHints.AtrTarget1Price,
                AtrTarget2Price = pattern.RiskHints.AtrTarget2Price,
                AtrRiskRewardRatio = pattern.RiskHints.AtrRiskRewardRatio,
                PositionSizePercent = pattern.RiskHints.PositionSizePercent,
                VolumeRatio = riskContext?.VolumeRatio ?? 1m,
                VolumeConfirmation = riskContext?.VolumeConfirmation ?? VolumeConfirmation.Neutral
            };
        }

        private static AnalysisWindowViewModel? ResolveAnalysisWindow(
            PatternAssessmentContract? mainPattern,
            IEnumerable<PatternAssessmentContract> alternativePatterns)
        {
            var source = mainPattern ?? alternativePatterns.FirstOrDefault();
            if (source is null)
            {
                return null;
            }

            return new AnalysisWindowViewModel
            {
                Interval = source.AnalysisWindow.Interval,
                StartDate = source.AnalysisWindow.StartDate,
                EndDate = source.AnalysisWindow.EndDate,
                RequiredCandles = source.AnalysisWindow.RequiredCandles,
                ActualCandles = source.AnalysisWindow.ActualCandles
            };
        }

        private static string BuildOutcomeMessage(AnalysisResponseViewModel response)
        {
            return response.Outcome switch
            {
                AnalysisOutcome.NoCrediblePattern => response.NoCrediblePatternReason ?? "Aucun pattern compatible identifié.",
                AnalysisOutcome.InsufficientData => response.NoCrediblePatternReason ?? "Données insuffisantes pour l'analyse.",
                AnalysisOutcome.UnsupportedInstrument => response.NoCrediblePatternReason ?? "Instrument non supporté.",
                AnalysisOutcome.UnsupportedContext => response.NoCrediblePatternReason ?? "Contexte non supporté.",
                _ => string.IsNullOrWhiteSpace(response.ModelMessage) ? response.PedagogicalSummary : response.ModelMessage
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

        // Le niveau de risque affiché n'a de sens que pour une recommandation actionnable
        // (achat/vente) : un pattern purement informatif (hold/wait) est toujours classé
        // "Information" quelle que soit sa confiance, pour ne pas laisser croire à un risque
        // de position sur un signal qui n'engage à rien.
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
