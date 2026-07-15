using System.Text.Json;
using BackPredictFinance.Common.AnalysisV1;
using BackPredictFinance.Common.enums;
using BackPredictFinance.Datas.Entities;
using BackPredictFinance.Services.ClientFinanceServices.Analysis;
using BackPredictFinance.ViewModels.ClientFinanceViewModels.Analysis;
using BackPredictFinance.ViewModels.ClientFinanceViewModels.Assets;
using BackPredictFinance.ViewModels.ClientFinanceViewModels.History;
using BackPredictFinance.ViewModels.ClientFinanceViewModels.Instruments;
using BackPredictFinance.ViewModels.ClientFinanceViewModels.Readings;
using Microsoft.EntityFrameworkCore;
using AnalysisPatternAssessment = BackPredictFinance.Common.AnalysisV1.PatternAssessmentContract;

namespace BackPredictFinance.Services.ClientFinanceServices
{
    /// <summary>
    /// Centralise les projections backend vers les view models ClientFinance.
    /// </summary>
    public interface IClientFinanceProjectionService
    {
        /// <summary>
        /// Retourne le statut persisté utilisé pour les analyses terminées.
        /// </summary>
        AnalysisRunStatusEnum CompletedStatus { get; }
        /// <summary>
        /// Charge le dernier snapshot d'analyse disponible pour chaque actif demandé.
        /// </summary>
        Task<Dictionary<string, PersistedAnalysisSnapshotPayloadReadModel>> LoadLatestAnalysisByAssetIdAsync(List<string> assetIds, CancellationToken ct = default);
        /// <summary>
        /// Retourne le dernier statut PEA connu pour un actif.
        /// </summary>
        Task<PeaEligibilityStatusEnum> GetLatestPeaEligibilityStatusAsync(string assetId, CancellationToken ct = default);
        /// <summary>
        /// Retourne la dernière projection PEA complète disponible pour un actif.
        /// </summary>
        Task<LatestPeaEligibilityProjection?> GetLatestPeaEligibilityAsync(string assetId, CancellationToken ct = default);
        /// <summary>
        /// Tente de lire un snapshot d'analyse persisté depuis son payload brut.
        /// </summary>
        PersistedAnalysisSnapshotPayloadReadModel? TryReadSnapshot(string rawPayload);
        /// <summary>
        /// Construit l'identité projetée d'un instrument.
        /// </summary>
        InstrumentIdentityViewModel BuildInstrumentIdentity(Asset asset);
        /// <summary>
        /// Construit une lecture marché vide lorsqu'aucun snapshot n'est disponible.
        /// </summary>
        MarketReadingSummaryViewModel BuildEmptyMarketReading();
        /// <summary>
        /// Construit la synthèse marché à partir d'un snapshot et d'un pattern principal.
        /// </summary>
        MarketReadingSummaryViewModel BuildMarketReadingSummary(PersistedAnalysisSnapshotPayloadReadModel snapshot, AnalysisPatternAssessment? primaryPattern);
        /// <summary>
        /// Construit la synthèse de support liée au statut PEA.
        /// </summary>
        SupportReadingSummaryViewModel BuildSupportReadingSummary(PeaEligibilityStatusEnum peaStatus);
        /// <summary>
        /// Construit une recommandation par défaut en l'absence de recommandation persistée.
        /// </summary>
        RecommendationSummaryViewModel BuildDefaultRecommendation(bool holdsInstrument);
        /// <summary>
        /// Construit la recommandation projetée à partir de la vérité backend.
        /// </summary>
        RecommendationSummaryViewModel BuildRecommendationSummary(AnalysisRecommendation? recommendation, bool holdsInstrument, RecommendationStrengthEnum? strength);
        /// <summary>
        /// Projette le détail de confiance d'un pattern vers le view model client.
        /// </summary>
        ConfidenceBreakdownViewModel BuildConfidenceBreakdownViewModel(ConfidenceBreakdown breakdown);
        /// <summary>
        /// Construit l'indicateur de fraîcheur d'une donnée horodatée.
        /// </summary>
        FreshnessViewModel BuildFreshness(DateTime? checkedAtUtc);
        /// <summary>
        /// Construit la synthèse instrument destinée au front.
        /// </summary>
        InstrumentSummaryViewModel BuildInstrumentSummary(
            Asset asset,
            PeaEligibilityStatusEnum peaStatus,
            FreshnessViewModel freshness,
            bool hasPersistedAnalysis,
            string? latestAnalysisId,
            string? latestSnapshotId);
        /// <summary>
        /// Construit la lecture marché détaillée.
        /// </summary>
        MarketReadingViewModel BuildDetailedMarketReading(PersistedAnalysisSnapshotPayloadReadModel? snapshot, MarketReadingSummaryViewModel summary);
        /// <summary>
        /// Construit la lecture support détaillée.
        /// </summary>
        SupportReadingViewModel BuildDetailedSupportReading(LatestPeaEligibilityProjection? peaEligibility);
        /// <summary>
        /// Construit la lecture de situation personnelle liée à la position utilisateur.
        /// </summary>
        PersonalSituationReadingViewModel BuildPersonalSituation(
            bool holdsInstrument,
            decimal totalQuantityHeld,
            decimal? averageUnitCost,
            int? openLineCount,
            string currencyCode,
            RecommendationSummaryViewModel recommendation);
        /// <summary>
        /// Construit les liens de navigation associés à un instrument.
        /// </summary>
        InstrumentNavigationLinksViewModel BuildInstrumentNavigationLinks(string symbol);
        /// <summary>
        /// Construit une entrée d'historique projetée.
        /// </summary>
        HistoryItemViewModel BuildHistoryItem(
            Asset asset,
            string analysisId,
            PersistedAnalysisSnapshotPayloadReadModel snapshot,
            PeaEligibilityStatusEnum peaStatus);
        /// <summary>
        /// Convertit un outcome d'analyse backend vers le type frontend.
        /// </summary>
        TechnicalAnalysisOutcomeTypeEnum MapOutcome(AnalysisOutcome outcome);
        /// <summary>
        /// Retourne le libellé d'affichage associé à un outcome d'analyse.
        /// </summary>
        string GetOutcomeDisplayLabel(AnalysisOutcome outcome);
    }

    /// <summary>
    /// Implémente les projections et lectures dérivées des snapshots d'analyse.
    /// </summary>
    public sealed class ClientFinanceProjectionService : BaseService, IClientFinanceProjectionService
    {
        public ClientFinanceProjectionService(IServiceProvider serviceProvider)
            : base(serviceProvider)
        {
        }

        public AnalysisRunStatusEnum CompletedStatus => AnalysisRunStatusEnum.Completed;

        public async Task<Dictionary<string, PersistedAnalysisSnapshotPayloadReadModel>> LoadLatestAnalysisByAssetIdAsync(List<string> assetIds, CancellationToken ct = default)
        {
            var analysisRuns = await _financeDbContext.AnalysisRuns
                .AsNoTracking()
                .Where(x => x.UserId == GetRequiredCurrentUserId() && x.Status == CompletedStatus && assetIds.Contains(x.AssetId))
                .OrderByDescending(x => x.CompletedAtUtc ?? x.StartedAtUtc)
                .ToListAsync(ct);

            var response = new Dictionary<string, PersistedAnalysisSnapshotPayloadReadModel>(StringComparer.Ordinal);
            var latestRunIdByAssetId = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var analysisRun in analysisRuns)
            {
                if (response.ContainsKey(analysisRun.AssetId))
                {
                    continue;
                }

                var snapshot = TryReadSnapshot(analysisRun.RawPayload);
                if (snapshot == null)
                {
                    continue;
                }

                response[analysisRun.AssetId] = snapshot;
                latestRunIdByAssetId[analysisRun.AssetId] = analysisRun.Id;
            }

            await ApplyEarningsDatesAsync(response, latestRunIdByAssetId, ct);

            return response;
        }

        private async Task ApplyEarningsDatesAsync(
            Dictionary<string, PersistedAnalysisSnapshotPayloadReadModel> response,
            Dictionary<string, string> latestRunIdByAssetId,
            CancellationToken ct)
        {
            if (latestRunIdByAssetId.Count == 0)
            {
                return;
            }

            var runIds = latestRunIdByAssetId.Values.ToList();
            var earningsDateByRunId = await _financeDbContext.DecisionSignals
                .AsNoTracking()
                .Where(x => runIds.Contains(x.AnalysisRunId))
                .Select(x => new { x.AnalysisRunId, x.EarningsDateUtc })
                .ToDictionaryAsync(x => x.AnalysisRunId, x => x.EarningsDateUtc, ct);

            foreach (var (assetId, runId) in latestRunIdByAssetId)
            {
                if (earningsDateByRunId.TryGetValue(runId, out var earningsDateUtc))
                {
                    response[assetId].EarningsDateUtc = earningsDateUtc;
                }
            }
        }

        public async Task<PeaEligibilityStatusEnum> GetLatestPeaEligibilityStatusAsync(string assetId, CancellationToken ct = default)
        {
            return (await GetLatestPeaEligibilityAsync(assetId, ct))?.EligibilityStatus ?? PeaEligibilityStatusEnum.Unknown;
        }

        public async Task<LatestPeaEligibilityProjection?> GetLatestPeaEligibilityAsync(string assetId, CancellationToken ct = default)
        {
            return await _financeDbContext.AssetPeaEligibilities
                .AsNoTracking()
                .Where(x => x.AssetId == assetId)
                .OrderByDescending(x => x.CheckedUtc ?? x.CreatedAtUtc)
                .Select(x => new LatestPeaEligibilityProjection
                {
                    UniverseId = x.UniverseId,
                    EligibilityStatus = x.EligibilityStatus,
                    SourceReference = x.SourceReference,
                    CheckedUtc = x.CheckedUtc
                })
                .FirstOrDefaultAsync(ct);
        }

        public PersistedAnalysisSnapshotPayloadReadModel? TryReadSnapshot(string rawPayload)
        {
            if (string.IsNullOrWhiteSpace(rawPayload))
            {
                return null;
            }

            try
            {
                return JsonSerializer.Deserialize<PersistedAnalysisSnapshotPayloadReadModel>(rawPayload, AnalysisSnapshotJsonOptions.Shared);
            }
            catch (JsonException)
            {
                return null;
            }
        }

        public InstrumentIdentityViewModel BuildInstrumentIdentity(Asset asset)
        {
            return new InstrumentIdentityViewModel
            {
                InstrumentId = asset.Id,
                Symbol = asset.Symbol,
                DisplayName = asset.Name ?? asset.Symbol,
                AssetType = asset.AssetType.ToString().ToUpperInvariant(),
                Exchange = asset.Exchange,
                Currency = asset.Currency,
                CountryCode = asset.Country
            };
        }

        public MarketReadingSummaryViewModel BuildEmptyMarketReading()
        {
            return new MarketReadingSummaryViewModel
            {
                Outcome = TechnicalAnalysisOutcomeTypeEnum.NoCrediblePattern,
                OutcomeDisplayLabel = GetOutcomeDisplayLabel(AnalysisOutcome.NoCrediblePattern),
                ProgressStatus = PatternProgressStatusEnum.Absent,
                ValidationState = ValidationStateEnum.NotApplicable,
                Alternatives = []
            };
        }

        public MarketReadingSummaryViewModel BuildMarketReadingSummary(PersistedAnalysisSnapshotPayloadReadModel snapshot, AnalysisPatternAssessment? primaryPattern)
        {
            var alternatives = snapshot.PatternRows
                .Where(x => x.PatternAssessmentPayload != null)
                .OrderBy(x => x.DisplayRank)
                .Select(x => x.PatternAssessmentPayload)
                .Where(x => x.PatternId != (primaryPattern?.PatternId ?? string.Empty))
                .Select(x => new AlternativePatternViewModel
                {
                    PatternId = x.PatternId,
                    DisplayName = string.IsNullOrWhiteSpace(x.DisplayName) ? x.PatternId : x.DisplayName
                })
                .ToList();

            return new MarketReadingSummaryViewModel
            {
                Outcome = MapOutcome(snapshot.Outcome),
                OutcomeDisplayLabel = GetOutcomeDisplayLabel(snapshot.Outcome),
                PrimaryPatternId = primaryPattern?.PatternId,
                PrimaryPatternDisplayName = primaryPattern == null
                    ? null
                    : string.IsNullOrWhiteSpace(primaryPattern.DisplayName) ? primaryPattern.PatternId : primaryPattern.DisplayName,
                ProgressStatus = MapPatternProgress(primaryPattern?.Detection.Status),
                ConfidenceLabel = primaryPattern?.Scoring.ConfidenceLabel,
                RecommendationStrength = MapRecommendationStrength(primaryPattern?.Scoring.ConfidenceLabel),
                ValidationState = MapValidationState(primaryPattern?.Validation.State),
                InvalidationLevel = primaryPattern?.Invalidation.InvalidationLevel,
                RiskHint = primaryPattern?.RiskHints.PositioningNote,
                Alternatives = alternatives
            };
        }

        public SupportReadingSummaryViewModel BuildSupportReadingSummary(PeaEligibilityStatusEnum peaStatus)
        {
            return new SupportReadingSummaryViewModel
            {
                AvailabilityStatus = SupportAvailabilityStatusEnum.Unavailable,
                AvailabilityDisplayLabel = "Lecture support indisponible",
                PeaEligibilityStatus = peaStatus,
                PeaDisplayLabel = BuildPeaDisplayLabel(peaStatus)
            };
        }

        public RecommendationSummaryViewModel BuildDefaultRecommendation(bool holdsInstrument)
        {
            return new RecommendationSummaryViewModel
            {
                Kind = RecommendationKind.Wait,
                HoldingStatus = holdsInstrument ? HoldingStatusEnum.Held : HoldingStatusEnum.NotHeld,
                DisplayLabel = "Attendre",
                ExplanationSummary = "Aucune recommandation gouvernee n'est disponible tant qu'aucune analyse exploitable n'a ete persistee."
            };
        }

        public RecommendationSummaryViewModel BuildRecommendationSummary(AnalysisRecommendation? recommendation, bool holdsInstrument, RecommendationStrengthEnum? strength)
        {
            if (recommendation == null)
            {
                return BuildDefaultRecommendation(holdsInstrument);
            }

            var currentHoldingStatus = holdsInstrument ? HoldingStatusEnum.Held : HoldingStatusEnum.NotHeld;
            if (recommendation.HoldingContext != currentHoldingStatus)
            {
                return new RecommendationSummaryViewModel
                {
                    Kind = RecommendationKind.Wait,
                    HoldingStatus = currentHoldingStatus,
                    DisplayLabel = "Attendre",
                    ExplanationSummary = "La derniere recommandation persistee a ete calculee avec un contexte portefeuille different. Une nouvelle analyse est necessaire pour gouverner la recommandation actuelle."
                };
            }

            return new RecommendationSummaryViewModel
            {
                Kind = recommendation.Kind,
                HoldingStatus = currentHoldingStatus,
                DisplayLabel = BuildRecommendationDisplayLabel(recommendation.Kind, strength),
                ExplanationSummary = string.IsNullOrWhiteSpace(recommendation.Rationale) ? "Aucune justification" : recommendation.Rationale.Trim(),
                WarningText = recommendation.WarningText
            };
        }

        public ConfidenceBreakdownViewModel BuildConfidenceBreakdownViewModel(ConfidenceBreakdown breakdown)
        {
            ArgumentNullException.ThrowIfNull(breakdown);

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

        public FreshnessViewModel BuildFreshness(DateTime? checkedAtUtc)
        {
            var status = FreshnessClassifier.Classify(checkedAtUtc, DateTime.UtcNow);

            return status switch
            {
                FreshnessStatusEnum.Missing => new FreshnessViewModel
                {
                    Status = FreshnessStatusEnum.Missing,
                    DisplayLabel = "Donnees indisponibles"
                },
                FreshnessStatusEnum.Fresh => new FreshnessViewModel
                {
                    Status = FreshnessStatusEnum.Fresh,
                    CheckedAtUtc = checkedAtUtc,
                    DisplayLabel = "Donnees a jour"
                },
                FreshnessStatusEnum.Aging => new FreshnessViewModel
                {
                    Status = FreshnessStatusEnum.Aging,
                    CheckedAtUtc = checkedAtUtc,
                    DisplayLabel = "Donnees vieillissantes"
                },
                _ => new FreshnessViewModel
                {
                    Status = FreshnessStatusEnum.Stale,
                    CheckedAtUtc = checkedAtUtc,
                    DisplayLabel = "Donnees obsoletes"
                }
            };
        }

        public InstrumentSummaryViewModel BuildInstrumentSummary(
            Asset asset,
            PeaEligibilityStatusEnum peaStatus,
            FreshnessViewModel freshness,
            bool hasPersistedAnalysis,
            string? latestAnalysisId,
            string? latestSnapshotId)
        {
            return new InstrumentSummaryViewModel
            {
                Instrument = BuildInstrumentIdentity(asset),
                PerimeterLabel = "PEA_FR_EQUITIES / DAILY",
                PeaEligibilityStatus = peaStatus,
                PeaDisplayLabel = BuildPeaDisplayLabel(peaStatus),
                Freshness = freshness,
                HasPersistedAnalysis = hasPersistedAnalysis,
                AnalysisAvailabilityLabel = hasPersistedAnalysis
                    ? "Analyse persistee disponible"
                    : "Aucune analyse persistee disponible",
                LatestAnalysisId = latestAnalysisId,
                LatestSnapshotId = latestSnapshotId
            };
        }

        public MarketReadingViewModel BuildDetailedMarketReading(PersistedAnalysisSnapshotPayloadReadModel? snapshot, MarketReadingSummaryViewModel summary)
        {
            if (snapshot == null)
            {
                return new MarketReadingViewModel
                {
                    Outcome = summary.Outcome,
                    OutcomeDisplayLabel = summary.OutcomeDisplayLabel,
                    PrimaryPatternId = summary.PrimaryPatternId,
                    PrimaryPatternDisplayName = summary.PrimaryPatternDisplayName,
                    ProgressStatus = summary.ProgressStatus,
                    ConfidenceLabel = summary.ConfidenceLabel,
                    RecommendationStrength = summary.RecommendationStrength,
                    ValidationState = summary.ValidationState,
                    ValidationSummary = "Aucune analyse technique persistee n est disponible pour cet instrument.",
                    InvalidationLevel = summary.InvalidationLevel,
                    RiskHint = summary.RiskHint,
                    PedagogicalSummary = "Aucune analyse technique persistee n est disponible pour cet instrument.",
                    Alternatives = summary.Alternatives
                };
            }

            var primaryPattern = snapshot.PrimaryPattern;
            var validationSummary = string.IsNullOrWhiteSpace(primaryPattern?.Validation.Reason)
                ? summary.ValidationState switch
                {
                    ValidationStateEnum.Validated => "Le pattern est valide selon les regles persistantes du snapshot.",
                    ValidationStateEnum.NotValidated => "Le pattern n est pas encore valide selon le snapshot.",
                    _ => "Aucune validation applicable n est disponible pour ce snapshot."
                }
                : primaryPattern!.Validation.Reason.Trim();
            var pedagogicalSummary = string.IsNullOrWhiteSpace(snapshot.PedagogicalSummary)
                ? primaryPattern?.PedagogicalDescription ?? "Aucun resume pedagogique persiste n est disponible."
                : snapshot.PedagogicalSummary.Trim();

            return new MarketReadingViewModel
            {
                Outcome = summary.Outcome,
                OutcomeDisplayLabel = summary.OutcomeDisplayLabel,
                PrimaryPatternId = summary.PrimaryPatternId,
                PrimaryPatternDisplayName = summary.PrimaryPatternDisplayName,
                ProgressStatus = summary.ProgressStatus,
                ConfidenceLabel = summary.ConfidenceLabel,
                RecommendationStrength = summary.RecommendationStrength,
                ValidationState = summary.ValidationState,
                ValidationSummary = validationSummary,
                InvalidationLevel = summary.InvalidationLevel,
                RiskHint = summary.RiskHint,
                PedagogicalSummary = pedagogicalSummary,
                Alternatives = summary.Alternatives
            };
        }

        public SupportReadingViewModel BuildDetailedSupportReading(LatestPeaEligibilityProjection? peaEligibility)
        {
            var peaStatus = peaEligibility?.EligibilityStatus ?? PeaEligibilityStatusEnum.Unknown;
            var hasRegistryTruth = peaEligibility != null;

            return new SupportReadingViewModel
            {
                AvailabilityStatus = hasRegistryTruth ? SupportAvailabilityStatusEnum.Partial : SupportAvailabilityStatusEnum.Unavailable,
                AvailabilityDisplayLabel = hasRegistryTruth ? "Lecture support partielle" : "Lecture support indisponible",
                ScoringVersion = null,
                ActiveUniverseId = string.IsNullOrWhiteSpace(peaEligibility?.UniverseId) ? null : peaEligibility.UniverseId,
                PeaEligibilityStatus = peaStatus,
                PeaDisplayLabel = BuildPeaDisplayLabel(peaStatus),
                CoverageRatio = null,
                CompositeScore = null,
                MissingCategorySummaries = [],
                Notes = hasRegistryTruth
                    ? [
                        "Le statut PEA provient du registre persiste.",
                        "Aucun snapshot de scoring fondamental n est persiste dans la V1 actuelle."
                    ]
                    : [
                        "Aucun registre PEA persiste n est disponible pour cet instrument.",
                        "Aucun snapshot de scoring fondamental n est persiste dans la V1 actuelle."
                    ]
            };
        }

        public PersonalSituationReadingViewModel BuildPersonalSituation(
            bool holdsInstrument,
            decimal totalQuantityHeld,
            decimal? averageUnitCost,
            int? openLineCount,
            string currencyCode,
            RecommendationSummaryViewModel recommendation)
        {
            return new PersonalSituationReadingViewModel
            {
                HoldingStatus = holdsInstrument ? HoldingStatusEnum.Held : HoldingStatusEnum.NotHeld,
                HoldsInstrument = holdsInstrument,
                TotalQuantityHeld = totalQuantityHeld,
                AverageUnitCost = averageUnitCost,
                OpenLineCount = openLineCount,
                CurrencyCode = string.IsNullOrWhiteSpace(currencyCode) ? "EUR" : currencyCode.Trim(),
                Recommendation = recommendation,
                GuidanceSummary = holdsInstrument
                    ? $"La recommandation contextuelle s applique a une position deja detenue: {recommendation.DisplayLabel}."
                    : $"La recommandation contextuelle s applique a un instrument non detenu: {recommendation.DisplayLabel}."
            };
        }

        public InstrumentNavigationLinksViewModel BuildInstrumentNavigationLinks(string symbol)
        {
            var normalizedSymbol = (symbol ?? string.Empty).Trim().ToUpperInvariant();
            return new InstrumentNavigationLinksViewModel
            {
                HistoryUrl = $"/api/ClientFinance/instruments/{normalizedSymbol}/analysis-history",
                SimulationUrl = "/api/ClientFinance/simulation/run",
                ComparisonUrl = "/api/ClientFinance/snapshots/compare"
            };
        }

        public HistoryItemViewModel BuildHistoryItem(
            Asset asset,
            string analysisId,
            PersistedAnalysisSnapshotPayloadReadModel snapshot,
            PeaEligibilityStatusEnum peaStatus)
        {
            var marketReading = BuildMarketReadingSummary(snapshot, snapshot.PrimaryPattern);
            var recommendation = BuildRecommendationSummary(
                snapshot.Recommendation?.RecommendationPayload,
                snapshot.PortfolioContextSnapshot.HoldsInstrument,
                marketReading.RecommendationStrength);

            return new HistoryItemViewModel
            {
                AnalysisId = analysisId,
                SnapshotId = snapshot.SnapshotId,
                Instrument = BuildInstrumentIdentity(asset),
                TimestampUtc = snapshot.CompletedAtUtc,
                Outcome = MapOutcome(snapshot.Outcome),
                OutcomeDisplayLabel = GetOutcomeDisplayLabel(snapshot.Outcome),
                PrimaryPatternLabel = marketReading.PrimaryPatternDisplayName ?? marketReading.PrimaryPatternId,
                RecommendationSummary = recommendation.DisplayLabel,
                SupportAvailabilitySummary = "Lecture support non persistee dans le snapshot V1",
                PeaEligibilityStatus = peaStatus,
                PeaSummary = BuildPeaDisplayLabel(peaStatus),
                AnalysisEngineVersion = snapshot.AnalysisEngineVersion,
                RecommendationPolicyVersion = snapshot.RecommendationPolicyVersion,
                ExplanationPolicyVersion = snapshot.ExplanationPolicyVersion,
                DetailUrl = $"/api/ClientFinance/analysis/{analysisId}",
                HistoryUrl = $"/api/ClientFinance/instruments/{asset.Symbol}/analysis-history",
                ComparisonUrl = "/api/ClientFinance/snapshots/compare"
            };
        }

        public TechnicalAnalysisOutcomeTypeEnum MapOutcome(AnalysisOutcome outcome)
        {
            return outcome switch
            {
                AnalysisOutcome.CrediblePatternFound => TechnicalAnalysisOutcomeTypeEnum.CrediblePatternFound,
                AnalysisOutcome.MultipleCompatiblePatterns => TechnicalAnalysisOutcomeTypeEnum.MultipleCompatiblePatterns,
                AnalysisOutcome.NoCrediblePattern => TechnicalAnalysisOutcomeTypeEnum.NoCrediblePattern,
                AnalysisOutcome.InsufficientData => TechnicalAnalysisOutcomeTypeEnum.InsufficientData,
                AnalysisOutcome.UnsupportedInstrument => TechnicalAnalysisOutcomeTypeEnum.UnsupportedInstrument,
                AnalysisOutcome.UnsupportedContext => TechnicalAnalysisOutcomeTypeEnum.UnsupportedContext,
                _ => TechnicalAnalysisOutcomeTypeEnum.NoCrediblePattern
            };
        }

        public string GetOutcomeDisplayLabel(AnalysisOutcome outcome)
        {
            return outcome switch
            {
                AnalysisOutcome.CrediblePatternFound => "Pattern credible detecte",
                AnalysisOutcome.MultipleCompatiblePatterns => "Plusieurs patterns compatibles",
                AnalysisOutcome.NoCrediblePattern => "Aucun pattern credible retenu",
                AnalysisOutcome.InsufficientData => "Donnees insuffisantes pour analyser",
                AnalysisOutcome.UnsupportedInstrument => "Instrument hors perimetre V1",
                AnalysisOutcome.UnsupportedContext => "Contexte non pris en charge",
                _ => "Aucun pattern credible retenu"
            };
        }

        private string GetRequiredCurrentUserId()
        {
            if (!string.IsNullOrWhiteSpace(_currentUserId))
            {
                return _currentUserId;
            }

            throw new InvalidOperationException("Aucun utilisateur courant n'est disponible.");
        }

        private static PatternProgressStatusEnum MapPatternProgress(PatternStatus? status)
        {
            return status switch
            {
                PatternStatus.Forming => PatternProgressStatusEnum.Forming,
                PatternStatus.Monitoring => PatternProgressStatusEnum.Monitoring,
                PatternStatus.Confirmed => PatternProgressStatusEnum.Confirmed,
                PatternStatus.Invalidated => PatternProgressStatusEnum.Invalidated,
                PatternStatus.Completed => PatternProgressStatusEnum.Completed,
                _ => PatternProgressStatusEnum.Absent
            };
        }

        private static ValidationStateEnum MapValidationState(string? state)
        {
            return (state ?? string.Empty).Trim().ToUpperInvariant() switch
            {
                "VALIDATED" => ValidationStateEnum.Validated,
                "NOT_VALIDATED" => ValidationStateEnum.NotValidated,
                _ => ValidationStateEnum.NotApplicable
            };
        }

        private static RecommendationStrengthEnum? MapRecommendationStrength(string? confidenceLabel)
        {
            return (confidenceLabel ?? string.Empty).Trim().ToUpperInvariant() switch
            {
                "HIGH" => RecommendationStrengthEnum.High,
                "MEDIUM" => RecommendationStrengthEnum.Medium,
                "LOW" => RecommendationStrengthEnum.Low,
                _ => null
            };
        }

        private static string BuildRecommendationDisplayLabel(RecommendationKind kind, RecommendationStrengthEnum? strength)
        {
            var action = kind switch
            {
                RecommendationKind.Monitor => "Surveiller",
                RecommendationKind.Wait => "Attendre",
                RecommendationKind.Buy => "Acheter",
                RecommendationKind.Hold => "Conserver",
                RecommendationKind.Reinforce => "Renforcer",
                RecommendationKind.Lighten => "Alleger",
                RecommendationKind.Sell => "Vendre",
                _ => "Attendre"
            };

            if (strength == null)
            {
                return action;
            }

            return $"{action} - {strength.Value switch { RecommendationStrengthEnum.High => "fort", RecommendationStrengthEnum.Medium => "moyen", _ => "faible" }}";
        }

        private static string BuildPeaDisplayLabel(PeaEligibilityStatusEnum peaStatus)
        {
            return peaStatus switch
            {
                PeaEligibilityStatusEnum.ConfirmedEligible => "Eligibilite PEA confirmee",
                PeaEligibilityStatusEnum.ConfirmedIneligible => "Non eligible PEA confirmee",
                _ => "Eligibilite PEA non confirmee"
            };
        }
    }

}
