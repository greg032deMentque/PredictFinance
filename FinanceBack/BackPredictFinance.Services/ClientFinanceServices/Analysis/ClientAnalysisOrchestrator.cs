using BackPredictFinance.Common;
using BackPredictFinance.Common.AnalysisV1;
using BackPredictFinance.Common.enums;
using BackPredictFinance.Patterns.Contracts;
using BackPredictFinance.ViewModels.ClientFinanceViewModels.Analysis;
using Microsoft.Extensions.Logging;
using System.Net;
using BackPredictFinance.Common.MarketData;

namespace BackPredictFinance.Services.ClientFinanceServices.Analysis
{
    /// <summary>
    /// Orchestre l'exécution, l'évaluation et la persistance d'une analyse complète.
    /// </summary>
    public interface IAnalysisOrchestrator
    {
        /// <summary>
        /// Lance une analyse complète et retourne sa projection de réponse.
        /// </summary>
        Task<AnalysisResponseViewModel> RunAnalysisAsync(AnalysisRequest request, CancellationToken ct = default);
    }


    /// <summary>
    /// Implémente l'orchestration de bout en bout du flux d'analyse V1.
    /// </summary>
    public sealed class ClientAnalysisOrchestrator : IAnalysisOrchestrator
    {
        private readonly IAnalysisExecutionService _analysisExecutionService;
        private readonly IRiskEvaluationService _riskEvaluationService;
        private readonly IRecommendationPolicyService _recommendationPolicyService;
        private readonly IPedagogicalExplanationService _pedagogicalExplanationService;
        private readonly IAnalysisSnapshotPersistenceService _snapshotPersistenceService;
        private readonly IDegradedModeState _degradedModeState;
        private readonly ILogger<ClientAnalysisOrchestrator> _logger;

        public ClientAnalysisOrchestrator(
            IAnalysisExecutionService analysisExecutionService,
            IRiskEvaluationService riskEvaluationService,
            IRecommendationPolicyService recommendationPolicyService,
            IPedagogicalExplanationService pedagogicalExplanationService,
            IAnalysisSnapshotPersistenceService snapshotPersistenceService,
            IDegradedModeState degradedModeState,
            ILogger<ClientAnalysisOrchestrator> logger)
        {
            _analysisExecutionService = analysisExecutionService;
            _riskEvaluationService = riskEvaluationService;
            _recommendationPolicyService = recommendationPolicyService;
            _pedagogicalExplanationService = pedagogicalExplanationService;
            _snapshotPersistenceService = snapshotPersistenceService;
            _degradedModeState = degradedModeState;
            _logger = logger;
        }

        // Ordre du pipeline volontaire : (1) execution deterministe des patterns, (2) ajustement du
        // scoring par la confirmation de volume globale, (3) evaluation du risque par pattern,
        // (4) determination de l'issue (aucun/un/plusieurs patterns compatibles), (5) pedagogie,
        // (6) politique de recommandation, (7) persistance du snapshot, (8) construction du contexte
        // risque/technique a partir du snapshot persiste. Chaque etape depend du resultat de la
        // precedente ; inverser (7) et (8) casserait l'alerte earnings qui lit EarningsDateUtc sur
        // l'enregistrement persiste plutot que sur la requete.
        public async Task<AnalysisResponseViewModel> RunAnalysisAsync(AnalysisRequest request, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(request);

            var startedAtUtc = DateTime.UtcNow;
            var primaryResolvedPattern = ResolvePrimaryPattern(request);

            AnalysisExecutionArtifact executionArtifact;
            try
            {
                executionArtifact = await _analysisExecutionService.ExecuteAsync(request, ct);
            }
            catch (Exception ex)
            {
                var failedAtUtc = DateTime.UtcNow;
                _logger.LogError(ex,
                    "Echec d'execution de l'analyse pour {Symbol} (patterns demandes={PatternIds}).",
                    request.Instrument.Symbol, string.Join(",", request.ResolvedPatternIds));
                // Le snapshot d'echec est persiste avant de propager l'exception : meme si l'appelant
                // ne recoit qu'une erreur HTTP, l'historique conserve une trace de la tentative.
                await _snapshotPersistenceService.PersistFailedAnalysisAsync(request, primaryResolvedPattern, startedAtUtc, failedAtUtc, ex, ct);

                if (ex is CustomException customException)
                {
                    throw customException;
                }

                if (ex is HttpRequestException or TaskCanceledException or System.Text.Json.JsonException)
                {
                    throw new CustomException(
                        $"L'analyse V1 a echoue pour {request.Instrument.Symbol}: fournisseur de donnees de marche indisponible et aucun snapshot recent exploitable ({ex.GetType().Name}: {ex.Message}).",
                        "Données de marché momentanément indisponibles, réessayez plus tard.",
                        statusCode: HttpStatusCode.InternalServerError);
                }

                throw new CustomException(
                    $"L'analyse V1 a echoue pour {request.Instrument.Symbol} sur les patterns demandes: {ex.Message}",
                    "L'analyse V1 n'a pas pu etre calculee.",
                    statusCode: HttpStatusCode.InternalServerError);
            }

            // La confirmation de volume est calculee une seule fois sur l'ensemble des bougies puis
            // appliquee a chaque pattern execute : c'est un signal de marche global, pas propre a un pattern.
            var (_, globalVolumeConfirmation) = TechnicalIndicators.ComputeVolumeConfirmation(executionArtifact.Candles);

            foreach (var executedPattern in executionArtifact.Patterns)
            {
                _riskEvaluationService.ApplyVolumeConfidenceAdjustment(executedPattern.ContractAssessment.Scoring, globalVolumeConfirmation);
                executedPattern.ContractAssessment.RiskHints = _riskEvaluationService.EvaluatePrimaryRisk(executionArtifact, executedPattern.ContractAssessment);
            }

            var orderedPatterns = executionArtifact.GetOrderedPatterns();
            var compatiblePatterns = executionArtifact.GetCompatiblePatternAssessments();

            // L'issue globale de l'analyse depend uniquement du nombre de patterns juges compatibles
            // apres filtrage (pas du nombre de patterns executes) : 0 => pas de conviction exploitable,
            // >1 => ambiguite a arbitrer par l'utilisateur, 1 => cas nominal.
            var outcome = compatiblePatterns.Count switch
            {
                0 => AnalysisOutcome.NoCrediblePattern,
                > 1 => AnalysisOutcome.MultipleCompatiblePatterns,
                _ => AnalysisOutcome.CrediblePatternFound
            };

            if (outcome == AnalysisOutcome.NoCrediblePattern)
            {
                _logger.LogInformation(
                    "Aucun pattern compatible pour {Symbol} : candles={CandleCount}, phases=[{Phases}].",
                    request.Instrument.Symbol,
                    executionArtifact.Candles.Count,
                    string.Join(", ", orderedPatterns.Select(p => $"{p.ContractAssessment.PatternId}:{p.ContractAssessment.Detection.CurrentPhaseCode}")));
            }

            // L'explication pedagogique est generee pour TOUS les patterns ordonnes (pas seulement les
            // compatibles) afin que l'utilisateur comprenne aussi pourquoi un pattern a ete ecarte.
            foreach (var patternAssessment in orderedPatterns.Select(x => x.ContractAssessment))
            {
                patternAssessment.Explanation = _pedagogicalExplanationService.BuildPatternExplanation(
                    patternAssessment,
                    compatiblePatterns.Count > 1,
                    executionArtifact.ModelStatus != ModelStatusEnum.Go);
            }

            var recommendation = _recommendationPolicyService.EvaluateAnalysis(request, compatiblePatterns, outcome);
            var pedagogicalSummary = _pedagogicalExplanationService.BuildAnalysisSummary(outcome, compatiblePatterns, recommendation, request.PortfolioContext);
            var completedAtUtc = DateTime.UtcNow;
            var persisted = await _snapshotPersistenceService.PersistSuccessfulAnalysisAsync(
                request,
                primaryResolvedPattern,
                executionArtifact,
                recommendation,
                outcome,
                pedagogicalSummary,
                _pedagogicalExplanationService.PolicyVersion,
                startedAtUtc,
                completedAtUtc,
                ct);

            // EarningsDateUtc est lu sur l'enregistrement PERSISTE (et non sur la requete) : la
            // persistance peut enrichir/corriger cette date via les donnees fondamentales resolues.
            var riskContext = _riskEvaluationService.BuildRiskContext(executionArtifact);
            ApplyEarningsWarning(riskContext, persisted.EarningsDateUtc, recommendation.ReviewHorizonDays ?? 0, completedAtUtc);
            var technicalContext = BuildTechnicalContext(executionArtifact.Candles);

            var response = BuildAnalysisResponse(request, persisted, primaryResolvedPattern, executionArtifact, recommendation, outcome, pedagogicalSummary, riskContext, technicalContext);
            response.DataFreshnessStatus = _degradedModeState.Status;
            response.DataFreshnessCheckedAtUtc = _degradedModeState.CheckedAtUtc;
            return response;
        }


        private static void ApplyEarningsWarning(AnalysisRiskContext riskContext, DateTime? earningsDateUtc, int horizonDays, DateTime emissionDateUtc)
        {
            riskContext.NextEarningsDateUtc = earningsDateUtc;
            riskContext.EarningsWithinHorizonWarning = EarningsHorizonEvaluator.IsWithinHorizon(earningsDateUtc, horizonDays, emissionDateUtc);
        }

        private static AnalysisTechnicalContext BuildTechnicalContext(IReadOnlyList<TickerCandle> candles)
        {
            var rsi = TechnicalIndicators.ComputeRsi(candles);
            var (macdValue, macdSignal, macdHistogram, macdCross) = TechnicalIndicators.ComputeMacd(candles);
            var (regime, regimeWarning) = TechnicalIndicators.ComputeMarketRegime(candles);

            return new AnalysisTechnicalContext
            {
                Rsi14 = Math.Round(rsi, 2),
                RsiZone = TechnicalIndicators.ClassifyRsiZone(rsi),
                MacdValue = macdValue,
                MacdSignal = macdSignal,
                MacdHistogram = macdHistogram,
                MacdCross = macdCross,
                MarketRegime = regime,
                RegimeWarning = regimeWarning
            };
        }

        // Le pattern "primaire" est le premier identifiant resolu non vide de la requete : c'est celui
        // utilise pour tracer un echec avant meme que l'execution ait determine les patterns compatibles.
        private static ResolvedAnalysisPattern ResolvePrimaryPattern(AnalysisRequest request)
        {
            var primaryPatternId = request.ResolvedPatternIds
                .FirstOrDefault(patternId => !string.IsNullOrWhiteSpace(patternId));

            if (string.IsNullOrWhiteSpace(primaryPatternId))
            {
                throw new InvalidOperationException("Au moins un pattern resolu est obligatoire pour lancer l'analyse V1.");
            }

            return new ResolvedAnalysisPattern
            {
                PatternId = primaryPatternId.Trim(),
                ModelVersion = string.Empty,
                ModelDir = string.Empty
            };
        }

        private static AnalysisResponseViewModel BuildAnalysisResponse(
            AnalysisRequest request,
            PersistedAnalysisRecord persisted,
            ResolvedAnalysisPattern resolvedPattern,
            AnalysisExecutionArtifact executionArtifact,
            AnalysisRecommendation recommendation,
            AnalysisOutcome outcome,
            string pedagogicalSummary,
            AnalysisRiskContext riskContext,
            AnalysisTechnicalContext technicalContext)
        {
            var compatiblePatterns = executionArtifact.GetCompatiblePatternAssessments();

            var mainPattern = compatiblePatterns.FirstOrDefault();
            var alternativePatterns = compatiblePatterns.Skip(1).ToList();

            return new AnalysisResponseViewModel
            {
                AnalysisId = persisted.PublicId,
                GeneratedAtUtc = executionArtifact.GeneratedAtUtc,
                AsOfDate = request.HistoryEndDate,
                Outcome = outcome,
                Instrument = new Instrument
                {
                    InstrumentId = persisted.InstrumentId,
                    Symbol = persisted.Symbol,
                    ProviderSymbol = persisted.ProviderSymbol,
                    DisplayName = persisted.CompanyName,
                    MarketCode = persisted.MarketCode,
                    CountryCode = persisted.CountryCode,
                    CurrencyCode = persisted.CurrencyCode,
                    AssetType = persisted.AssetType,
                    IsActive = persisted.IsActive,
                    LastProfileSyncUtc = persisted.LastProfileSyncUtc,
                    Summary = persisted.Summary
                },
                RequestedPatternIds = request.RequestedPatternIds,
                ExecutedPatternIds = executionArtifact.GetExecutedPatternIds(resolvedPattern.PatternId),
                MainPattern = mainPattern,
                AlternativePatterns = alternativePatterns,
                Recommendation = recommendation,
                PedagogicalSummary = pedagogicalSummary,
                NoCrediblePatternReason = outcome == AnalysisOutcome.NoCrediblePattern
                    ? "Aucun pattern compatible n'a ete identifie."
                    : null,
                Trace = new AnalysisResponseTrace
                {
                    TraceId = persisted.PublicId,
                    AnalysisEngineVersion = executionArtifact.ResolveAnalysisEngineVersion(resolvedPattern.ModelVersion),
                    RuleSetVersion = executionArtifact.ResolveRuleSetVersion(resolvedPattern.ModelVersion)
                },
                Warnings = executionArtifact.ModelStatus == ModelStatusEnum.Go
                    ? []
                    : [string.IsNullOrWhiteSpace(executionArtifact.ModelMessage) ? "Le moteur d'analyse n'est pas pleinement valide." : executionArtifact.ModelMessage],
                ModelStatus = executionArtifact.ModelStatus,
                ModelMessage = string.IsNullOrWhiteSpace(executionArtifact.ModelMessage) ? string.Empty : executionArtifact.ModelMessage.Trim(),
                Candles = executionArtifact.Candles.ToList(),
                RiskContext = riskContext,
                TechnicalContext = technicalContext
            };
        }
    }
}
