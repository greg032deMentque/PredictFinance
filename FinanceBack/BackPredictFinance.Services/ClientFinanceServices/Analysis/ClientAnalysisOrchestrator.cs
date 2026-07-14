using BackPredictFinance.Common;
using BackPredictFinance.Common.AnalysisV1;
using BackPredictFinance.Common.enums;
using BackPredictFinance.Patterns.Contracts;
using BackPredictFinance.ViewModels.ClientFinanceViewModels.Analysis;
using System.Net;

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

        public ClientAnalysisOrchestrator(
            IAnalysisExecutionService analysisExecutionService,
            IRiskEvaluationService riskEvaluationService,
            IRecommendationPolicyService recommendationPolicyService,
            IPedagogicalExplanationService pedagogicalExplanationService,
            IAnalysisSnapshotPersistenceService snapshotPersistenceService)
        {
            _analysisExecutionService = analysisExecutionService;
            _riskEvaluationService = riskEvaluationService;
            _recommendationPolicyService = recommendationPolicyService;
            _pedagogicalExplanationService = pedagogicalExplanationService;
            _snapshotPersistenceService = snapshotPersistenceService;
        }

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
                await _snapshotPersistenceService.PersistFailedAnalysisAsync(request, primaryResolvedPattern, startedAtUtc, failedAtUtc, ex, ct);

                if (ex is CustomException customException)
                {
                    throw customException;
                }

                throw new CustomException(
                    $"L'analyse V1 a echoue pour {request.Instrument.Symbol} sur les patterns demandes: {ex.Message}",
                    "L'analyse V1 n'a pas pu etre calculee.",
                    statusCode: HttpStatusCode.InternalServerError);
            }

            foreach (var executedPattern in executionArtifact.Patterns)
            {
                executedPattern.ContractAssessment.RiskHints = _riskEvaluationService.EvaluatePrimaryRisk(executionArtifact, executedPattern.ContractAssessment);
            }

            var orderedPatterns = executionArtifact.GetOrderedPatterns();
            var compatiblePatterns = executionArtifact.GetCompatiblePatternAssessments();

            var outcome = compatiblePatterns.Count switch
            {
                0 => AnalysisOutcome.NoCrediblePattern,
                > 1 => AnalysisOutcome.MultipleCompatiblePatterns,
                _ => AnalysisOutcome.CrediblePatternFound
            };

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

            return BuildAnalysisResponse(request, persisted, primaryResolvedPattern, executionArtifact, recommendation, outcome, pedagogicalSummary);
        }


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
            string pedagogicalSummary)
        {
            var orderedPatterns = executionArtifact.GetOrderedPatterns();
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
                ModelMessage = string.IsNullOrWhiteSpace(executionArtifact.ModelMessage) ? string.Empty : executionArtifact.ModelMessage.Trim()
            };
        }
    }
}
