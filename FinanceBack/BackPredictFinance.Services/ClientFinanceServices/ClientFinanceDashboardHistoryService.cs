using BackPredictFinance.Common.AnalysisV1;
using BackPredictFinance.Common.enums;
using BackPredictFinance.Patterns;
using BackPredictFinance.Services.ClientFinanceServices.Analysis;
using BackPredictFinance.Datas.Entities;
using BackPredictFinance.ViewModels.ClientFinanceViewModels;
using Microsoft.EntityFrameworkCore;
using BackPredictFinance.ViewModels.ClientFinanceViewModels.Analysis;
using BackPredictFinance.ViewModels.ClientFinanceViewModels.Dashboard;
using BackPredictFinance.ViewModels.ClientFinanceViewModels.Readings;

namespace BackPredictFinance.Services.ClientFinanceServices
{
    /// <summary>
    /// Produit les lectures de dashboard et les vues d'historique récentes.
    /// </summary>
    public interface IClientFinanceDashboardHistoryService
    {
        /// <summary>
        /// Retourne le dashboard synthétique de l'utilisateur courant.
        /// </summary>
        Task<ClientDashboardViewModel> GetDashboardAsync(CancellationToken ct = default);
        /// <summary>
        /// Retourne les analyses récentes projetées pour le dashboard.
        /// </summary>
        Task<List<AnalysisResultViewModel>> GetRecentAnalysesAsync(int take, CancellationToken ct = default);
        /// <summary>
        /// Retourne le détail projeté d'une analyse historisée.
        /// </summary>
        Task<AnalysisDetailViewModel?> GetAnalysisDetailAsync(string analysisId, CancellationToken ct = default);
    }

    /// <summary>
    /// Implémente les projections de dashboard et d'historique récentes côté client finance.
    /// </summary>
    public sealed class ClientFinanceDashboardHistoryService : BaseService, IClientFinanceDashboardHistoryService
    {
        private readonly IClientFinanceAssetSupportService _assetSupportService;
        private readonly IClientFinanceProjectionService _projectionService;
        private readonly IClientFinanceWatchlistPortfolioService _watchlistPortfolioService;
        private readonly IAnalysisResultProjectionService _analysisResultProjectionService;
        private readonly IConfidenceBreakdownAssembler _confidenceBreakdownAssembler;
        private readonly IActionPlanGenerationService _actionPlanGenerationService;

        public ClientFinanceDashboardHistoryService(
            IServiceProvider serviceProvider,
            IClientFinanceAssetSupportService assetSupportService,
            IClientFinanceProjectionService projectionService,
            IClientFinanceWatchlistPortfolioService watchlistPortfolioService,
            IAnalysisResultProjectionService analysisResultProjectionService,
            IConfidenceBreakdownAssembler confidenceBreakdownAssembler,
            IActionPlanGenerationService actionPlanGenerationService)
            : base(serviceProvider)
        {
            _assetSupportService = assetSupportService;
            _projectionService = projectionService;
            _watchlistPortfolioService = watchlistPortfolioService;
            _analysisResultProjectionService = analysisResultProjectionService;
            _confidenceBreakdownAssembler = confidenceBreakdownAssembler;
            _actionPlanGenerationService = actionPlanGenerationService;
        }

        public async Task<ClientDashboardViewModel> GetDashboardAsync(CancellationToken ct = default)
        {
            var watchlist = await _watchlistPortfolioService.GetWatchlistAsync(ct);
            var portfolio = await _watchlistPortfolioService.GetPortfolioAsync(null, ct);
            var analysesWeekStart = DateTime.UtcNow.AddDays(-7);

            var userAssetIds = await _financeDbContext.UserAssets
                .Where(x => x.UserId == _currentUserId)
                .Select(x => x.Id)
                .ToListAsync(ct);

            var analysesThisWeek = await _financeDbContext.Set<Recommendation>()
                .AsNoTracking()
                .CountAsync(x => userAssetIds.Contains(x.UserAssetId) && x.RecommendedAtUtc >= analysesWeekStart, ct);

            var recentAnalyses = await BuildDashboardRecentAnalysesAsync(5, ct);
            var attentionItems = watchlist
                .OrderByDescending(x => x.HoldingStatus == HoldingStatusEnum.Held)
                .ThenByDescending(x => x.Recommendation.Kind == RecommendationKind.Buy || x.Recommendation.Kind == RecommendationKind.Reinforce)
                .ThenBy(x => x.Instrument.Symbol, StringComparer.Ordinal)
                .Take(6)
                .Select(x => new DashboardAttentionItemViewModel
                {
                    Instrument = x.Instrument,
                    HoldingStatus = x.HoldingStatus,
                    MarketReading = x.MarketReading,
                    SupportReading = x.SupportReading,
                    Recommendation = x.Recommendation,
                    Freshness = x.Freshness
                })
                .ToList();

            var incompleteItems = recentAnalyses
                .Where(x => x.Outcome is not TechnicalAnalysisOutcomeTypeEnum.CrediblePatternFound and not TechnicalAnalysisOutcomeTypeEnum.MultipleCompatiblePatterns)
                .Select(x => new DashboardIncompleteItemViewModel
                {
                    AnalysisId = x.AnalysisId,
                    Instrument = x.Instrument,
                    Outcome = x.Outcome,
                    OutcomeDisplayLabel = x.MarketReading.OutcomeDisplayLabel,
                    ExplanationSummary = x.Recommendation.ExplanationSummary
                })
                .ToList();

            var dayProfitLoss = watchlist.Sum(x => x.OutstandingAmount * (x.DayVariationPct / 100m));

            return new ClientDashboardViewModel
            {
                TotalPortfolioValue = decimal.Round(portfolio.TotalOutstandingAmount, 2),
                DayProfitLoss = decimal.Round(dayProfitLoss, 2),
                OpenPositions = portfolio.OpenPositionCount,
                AnalysesThisWeek = analysesThisWeek,
                WatchlistCount = watchlist.Count,
                NextMarketOpenAt = ComputeNextMarketOpenUtc(),
                TotalInvested = decimal.Round(portfolio.TotalInvestedAmount, 2),
                TotalOutstanding = decimal.Round(portfolio.TotalOutstandingAmount, 2),
                AttentionItems = attentionItems,
                RecentAnalyses = recentAnalyses,
                IncompleteItems = incompleteItems,
                QuickAnalyzeEntry = new QuickAnalyzeEntryViewModel
                {
                    RuntimePerimeterLabel = "PEA_FR_EQUITIES",
                    SupportedPatternIds = PatternCatalog.GetTargetPatterns().Select(x => x.PatternId).ToList()
                }
            };
        }

        public Task<List<AnalysisResultViewModel>> GetRecentAnalysesAsync(int take, CancellationToken ct = default)
            => _analysisResultProjectionService.GetRecentAnalysesAsync(_assetSupportService.GetRequiredCurrentUserId(), take, ct);

        public async Task<AnalysisDetailViewModel?> GetAnalysisDetailAsync(string analysisId, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(analysisId))
            {
                throw new ArgumentException("L'identifiant d'analyse est obligatoire.", nameof(analysisId));
            }

            var analysisRun = await _financeDbContext.AnalysisRuns
                .AsNoTracking()
                .Include(x => x.Asset)
                .FirstOrDefaultAsync(x => x.UserId == _assetSupportService.GetRequiredCurrentUserId() && x.Id == analysisId && x.Status == _projectionService.CompletedStatus, ct);

            if (analysisRun == null)
            {
                return null;
            }

            var snapshot = _projectionService.TryReadSnapshot(analysisRun.RawPayload);
            if (snapshot == null)
            {
                return null;
            }

            var marketReading = _projectionService.BuildMarketReadingSummary(snapshot, snapshot.PrimaryPattern);
            var peaStatus = await _projectionService.GetLatestPeaEligibilityStatusAsync(analysisRun.AssetId, ct);
            var supportReading = _projectionService.BuildSupportReadingSummary(peaStatus);
            var recommendation = _projectionService.BuildRecommendationSummary(
                snapshot.Recommendation?.RecommendationPayload,
                snapshot.PortfolioContextSnapshot.HoldsInstrument,
                marketReading.RecommendationStrength);
            var patternLabel = marketReading.PrimaryPatternDisplayName ?? marketReading.PrimaryPatternId ?? "Analyse";
            var outcomeLabel = _projectionService.GetOutcomeDisplayLabel(snapshot.Outcome);

            var holdsInstrument = snapshot.PortfolioContextSnapshot.HoldsInstrument;
            var confidenceBreakdown = _confidenceBreakdownAssembler.Build(snapshot.PrimaryPattern);
            var actionPlan = _actionPlanGenerationService.Generate(
                snapshot.Outcome,
                snapshot.PrimaryPattern,
                snapshot.Recommendation?.RecommendationPayload,
                holdsInstrument,
                snapshot.InstrumentSnapshot.CurrencyCode);

            var exPostEvaluation = await BuildExPostEvaluationAsync(
                analysisRun.AssetId,
                snapshot.CompletedAtUtc,
                snapshot.Recommendation?.RecommendationPayload?.ReviewHorizonDays,
                snapshot.PrimaryPattern,
                ct);

            return new AnalysisDetailViewModel
            {
                AnalysisId = analysisRun.Id,
                Instrument = _projectionService.BuildInstrumentIdentity(analysisRun.Asset),
                GeneratedAtUtc = snapshot.CompletedAtUtc,
                Outcome = _projectionService.MapOutcome(snapshot.Outcome),
                OutcomeDisplayLabel = outcomeLabel,
                MarketReading = marketReading,
                ConfidenceBreakdown = MapConfidenceBreakdown(confidenceBreakdown),
                SupportReading = supportReading,
                Recommendation = recommendation,
                ActionPlan = MapActionPlan(actionPlan, holdsInstrument),
                WhyRecommendation = recommendation.ExplanationSummary,
                PedagogicalSummary = string.IsNullOrWhiteSpace(snapshot.PedagogicalSummary) ? recommendation.ExplanationSummary : snapshot.PedagogicalSummary,
                SnapshotId = snapshot.SnapshotId,
                HistoryRoute = $"/api/ClientFinance/instruments/{analysisRun.Asset.Symbol}/analysis-history",
                CompactSummary = $"{patternLabel} · {recommendation.DisplayLabel}",
                ModelMessage = snapshot.ModelSnapshot.ModelMessage,
                ExPostEvaluation = exPostEvaluation
            };
        }

        private async Task<ExPostEvaluationViewModel> BuildExPostEvaluationAsync(
            string assetId,
            DateTime analysisCompletedAtUtc,
            int? reviewHorizonDays,
            BackPredictFinance.Common.AnalysisV1.PatternAssessmentContract? primaryPattern,
            CancellationToken ct)
        {
            if (reviewHorizonDays is null or <= 0 || primaryPattern == null)
            {
                return ExPostEvaluationViewModel.NotApplicable();
            }

            var reviewScheduledAtUtc = analysisCompletedAtUtc.AddDays(reviewHorizonDays.Value);
            var targetPrice = primaryPattern.RiskHints.SuggestedTakeProfit;
            var invalidationPrice = primaryPattern.Invalidation.InvalidationLevel;

            if (!targetPrice.HasValue && !invalidationPrice.HasValue)
            {
                return ExPostEvaluationViewModel.NotApplicable();
            }

            if (reviewScheduledAtUtc > DateTime.UtcNow)
            {
                return ExPostEvaluationViewModel.Pending(reviewScheduledAtUtc);
            }

            var scanStart = analysisCompletedAtUtc.Date;
            var scanEnd = reviewScheduledAtUtc.AddDays(3);

            var candles = await _financeDbContext.AssetCandleSnapshots
                .AsNoTracking()
                .Where(c => c.AssetId == assetId && c.Interval == "1d"
                    && c.TimestampUtc >= scanStart && c.TimestampUtc <= scanEnd)
                .OrderBy(c => c.TimestampUtc)
                .ToListAsync(ct);

            if (candles.Count == 0)
            {
                return ExPostEvaluationViewModel.DataUnavailable(reviewScheduledAtUtc);
            }

            var hit = SignalDirectionalScanEvaluator.ScanForFirstHit(candles, primaryPattern.Direction, targetPrice, invalidationPrice);

            if (hit.Kind == SignalDirectionalHitKind.InvalidationHit)
            {
                var invalidationValue = invalidationPrice!.Value;
                return ExPostEvaluationViewModel.PathDependent(
                    reviewScheduledAtUtc,
                    "INVALIDATED",
                    "Invalidation déclenchée",
                    hit.Candle!.Close,
                    targetPrice,
                    invalidationPrice,
                    hit.CandleIndex,
                    hit.Candle.TimestampUtc,
                    $"L'invalidation ({invalidationValue:N2}) a été franchie le {hit.Candle.TimestampUtc:dd/MM/yyyy} (bougie {hit.CandleIndex + 1}). Le scénario ne s'est pas réalisé — c'est une leçon utile.");
            }

            if (hit.Kind == SignalDirectionalHitKind.TargetHit)
            {
                var targetValue = targetPrice!.Value;
                var directionLabel = primaryPattern.Direction == PatternDirectionEnum.Bearish ? "baissier" : "haussier";
                return ExPostEvaluationViewModel.PathDependent(
                    reviewScheduledAtUtc,
                    "TARGET_REACHED",
                    "Cible atteinte",
                    hit.Candle!.Close,
                    targetPrice,
                    invalidationPrice,
                    hit.CandleIndex,
                    hit.Candle.TimestampUtc,
                    $"La cible ({targetValue:N2}) a été atteinte le {hit.Candle.TimestampUtc:dd/MM/yyyy} (bougie {hit.CandleIndex + 1}). Le scénario {directionLabel} s'est réalisé.");
            }

            return new ExPostEvaluationViewModel
            {
                Status = "PENDING",
                StatusLabel = "En cours",
                ReviewScheduledAtUtc = reviewScheduledAtUtc,
                TargetPrice = targetPrice,
                InvalidationPrice = invalidationPrice,
                PedagogicalNote = $"Aucun niveau n'a été touché sur les {candles.Count} bougies disponibles. Le scénario reste ouvert."
            };
        }

        private async Task<List<DashboardRecentAnalysisItemViewModel>> BuildDashboardRecentAnalysesAsync(int take, CancellationToken ct)
        {
            var size = Math.Clamp(take, 1, 20);
            var analysisRuns = await _financeDbContext.AnalysisRuns
                .AsNoTracking()
                .Include(x => x.Asset)
                .Where(x => x.UserId == _assetSupportService.GetRequiredCurrentUserId() && x.Status == _projectionService.CompletedStatus)
                .OrderByDescending(x => x.CompletedAtUtc ?? x.StartedAtUtc)
                .Take(size)
                .ToListAsync(ct);

            var result = new List<DashboardRecentAnalysisItemViewModel>(analysisRuns.Count);
            foreach (var analysisRun in analysisRuns)
            {
                var snapshot = _projectionService.TryReadSnapshot(analysisRun.RawPayload);
                if (snapshot == null)
                {
                    continue;
                }

                var marketReading = _projectionService.BuildMarketReadingSummary(snapshot, snapshot.PrimaryPattern);
                var peaStatus = await _projectionService.GetLatestPeaEligibilityStatusAsync(analysisRun.AssetId, ct);

                result.Add(new DashboardRecentAnalysisItemViewModel
                {
                    AnalysisId = analysisRun.Id,
                    Instrument = _projectionService.BuildInstrumentIdentity(analysisRun.Asset),
                    TimestampUtc = snapshot.CompletedAtUtc,
                    Outcome = _projectionService.MapOutcome(snapshot.Outcome),
                    MarketReading = marketReading,
                    SupportReading = _projectionService.BuildSupportReadingSummary(peaStatus),
                    Recommendation = _projectionService.BuildRecommendationSummary(
                        snapshot.Recommendation?.RecommendationPayload,
                        snapshot.PortfolioContextSnapshot.HoldsInstrument,
                        marketReading.RecommendationStrength),
                    Freshness = _projectionService.BuildFreshness(snapshot.CompletedAtUtc)
                });
            }

            return result;
        }

        private static ConfidenceBreakdownViewModel MapConfidenceBreakdown(ConfidenceBreakdown breakdown)
        {
            return new ConfidenceBreakdownViewModel
            {
                Level = breakdown.Label,
                Criteria = breakdown.Criteria
                    .Select(criterion => new ConfidenceCriterionViewModel
                    {
                        Code = criterion.Code,
                        Label = criterion.Label,
                        State = MapCriterionStateToken(criterion.State),
                        Source = MapCriterionSourceToken(criterion.Source)
                    })
                    .ToList()
            };
        }

        private static ActionPlanViewModel MapActionPlan(ActionPlan actionPlan, bool holdsInstrument)
        {
            return new ActionPlanViewModel
            {
                HoldingStatus = holdsInstrument ? HoldingStatusEnum.Held : HoldingStatusEnum.NotHeld,
                PolicyVersion = actionPlan.PolicyVersion,
                Steps = actionPlan.Steps
                    .Select(step => new ActionPlanStepViewModel
                    {
                        Kind = MapActionStepKindToken(step.Kind),
                        Label = step.Label,
                        Source = step.SourceField,
                        Value = step.Value,
                        AlertTrigger = step.AlertTrigger.HasValue ? MapAlertTriggerToken(step.AlertTrigger.Value) : null
                    })
                    .ToList()
            };
        }

        private static string MapCriterionStateToken(CriterionState state)
        {
            return state switch
            {
                CriterionState.Met => "met",
                CriterionState.Partial => "partial",
                _ => "absent"
            };
        }

        private static string MapCriterionSourceToken(CriterionSource source)
        {
            return source switch
            {
                CriterionSource.Detection => "DETECTION",
                CriterionSource.Validation => "VALIDATION",
                _ => "INVALIDATION"
            };
        }

        private static string MapActionStepKindToken(ActionStepKind kind)
        {
            return kind switch
            {
                ActionStepKind.NoteLevel => "NOTE_LEVEL",
                ActionStepKind.ReviewAt => "REVIEW_AT",
                ActionStepKind.SetAlert => "SET_ALERT",
                ActionStepKind.HoldingReminder => "HOLDING_REMINDER",
                _ => "WAIT_FOR_DATA"
            };
        }

        private static string MapAlertTriggerToken(AlertTrigger alertTrigger)
        {
            return alertTrigger switch
            {
                AlertTrigger.PatternStateChange => "PATTERN_STATE_CHANGE",
                AlertTrigger.LevelCrossed => "LEVEL_CROSSED",
                _ => "DATA_STALE"
            };
        }

        private static DateTime ComputeNextMarketOpenUtc()
        {
            var parisTimeZone = ResolveParisTimeZone();
            var nowUtc = DateTime.UtcNow;
            var nowParis = TimeZoneInfo.ConvertTimeFromUtc(nowUtc, parisTimeZone);
            var nextParisOpen = new DateTime(nowParis.Year, nowParis.Month, nowParis.Day, 9, 0, 0, DateTimeKind.Unspecified);

            if (nowParis >= nextParisOpen)
            {
                nextParisOpen = nextParisOpen.AddDays(1);
            }

            while (nextParisOpen.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
            {
                nextParisOpen = nextParisOpen.AddDays(1);
            }

            return TimeZoneInfo.ConvertTimeToUtc(nextParisOpen, parisTimeZone);
        }

        private static TimeZoneInfo ResolveParisTimeZone()
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById("Romance Standard Time");
            }
            catch (TimeZoneNotFoundException)
            {
                return TimeZoneInfo.FindSystemTimeZoneById("Europe/Paris");
            }
        }
    }
}
