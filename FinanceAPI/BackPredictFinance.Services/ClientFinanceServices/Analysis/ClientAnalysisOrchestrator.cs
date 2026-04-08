using BackPredictFinance.Common.Common;
using BackPredictFinance.Common.enums;
using BackPredictFinance.Contracts.Analysis;
using System.Net;

namespace BackPredictFinance.Services.ClientFinanceServices.Analysis
{
    public interface IAnalysisOrchestrator
    {
        Task<AnalysisResponse> RunAnalysisAsync(AnalysisRequest request, CancellationToken ct = default);
    }

    public sealed class ClientAnalysisOrchestrator : IAnalysisOrchestrator
    {
        private readonly IAnalysisPatternRegistry _patternRegistry;
        private readonly IAnalysisExecutionService _analysisExecutionService;
        private readonly IRiskEvaluationService _riskEvaluationService;
        private readonly IRecommendationPolicyService _recommendationPolicyService;
        private readonly IPedagogicalExplanationService _pedagogicalExplanationService;
        private readonly IAnalysisSnapshotPersistenceService _snapshotPersistenceService;

        public ClientAnalysisOrchestrator(
            IAnalysisPatternRegistry patternRegistry,
            IAnalysisExecutionService analysisExecutionService,
            IRiskEvaluationService riskEvaluationService,
            IRecommendationPolicyService recommendationPolicyService,
            IPedagogicalExplanationService pedagogicalExplanationService,
            IAnalysisSnapshotPersistenceService snapshotPersistenceService)
        {
            _patternRegistry = patternRegistry;
            _analysisExecutionService = analysisExecutionService;
            _riskEvaluationService = riskEvaluationService;
            _recommendationPolicyService = recommendationPolicyService;
            _pedagogicalExplanationService = pedagogicalExplanationService;
            _snapshotPersistenceService = snapshotPersistenceService;
        }

        public async Task<AnalysisResponse> RunAnalysisAsync(AnalysisRequest request, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(request);

            var startedAtUtc = DateTime.UtcNow;
            var resolvedPatterns = request.ResolvedPatternIds
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => _patternRegistry.ResolveRequestedPattern(x))
                .GroupBy(x => x.PatternId, StringComparer.OrdinalIgnoreCase)
                .Select(group => group.First())
                .ToList();
            var primaryResolvedPattern = resolvedPatterns.FirstOrDefault() ?? throw new InvalidOperationException("Aucun pattern actif n'a ete resolu pour l'analyse.");

            AnalysisExecutionArtifact executionArtifact;
            try
            {
                executionArtifact = await _analysisExecutionService.ExecuteAsync(request, resolvedPatterns, ct);
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
                    $"L'analyse V1 a echoue pour {request.Instrument.Symbol} sur le pattern {primaryResolvedPattern.PatternId}: {ex.Message}",
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
                0 => AnalysisOutcomeEnum.NoCrediblePattern,
                > 1 => AnalysisOutcomeEnum.MultipleCompatiblePatterns,
                _ => AnalysisOutcomeEnum.CrediblePatternFound
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

        private static AnalysisResponse BuildAnalysisResponse(
            AnalysisRequest request,
            PersistedAnalysisRecord persisted,
            ResolvedAnalysisPattern primaryResolvedPattern,
            AnalysisExecutionArtifact executionArtifact,
            AnalysisRecommendation recommendation,
            AnalysisOutcomeEnum outcome,
            string pedagogicalSummary)
        {
            var compatiblePatterns = executionArtifact.GetCompatiblePatternAssessments();
            var mainPattern = compatiblePatterns.FirstOrDefault();
            var alternativePatterns = compatiblePatterns.Skip(1).ToList();

            return new AnalysisResponse
            {
                AnalysisId = persisted.PublicId,
                GeneratedAtUtc = executionArtifact.GeneratedAtUtc,
                AsOfDate = request.HistoryEndDate,
                AnalysisOutcome = outcome,
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
                ExecutedPatternIds = executionArtifact.GetExecutedPatternIds(primaryResolvedPattern.PatternId),
                MainPattern = mainPattern,
                AlternativePatterns = alternativePatterns,
                Recommendation = recommendation,
                PedagogicalSummary = pedagogicalSummary,
                NoCrediblePatternReason = outcome == AnalysisOutcomeEnum.NoCrediblePattern ? "Aucun pattern compatible n'a ete identifie." : null,
                Trace = new AnalysisResponseTrace
                {
                    TraceId = persisted.PublicId,
                    AnalysisEngineVersion = executionArtifact.ResolveAnalysisEngineVersion(primaryResolvedPattern.ModelVersion),
                    RuleSetVersion = executionArtifact.ResolveRuleSetVersion(primaryResolvedPattern.ModelVersion)
                },
                Warnings = executionArtifact.ModelStatus == ModelStatusEnum.Go ? [] : [string.IsNullOrWhiteSpace(executionArtifact.ModelMessage) ? "Le modele legacy n'est pas pleinement valide." : executionArtifact.ModelMessage],
                ModelStatus = executionArtifact.ModelStatus,
                ModelMessage = string.IsNullOrWhiteSpace(executionArtifact.ModelMessage) ? string.Empty : executionArtifact.ModelMessage.Trim()
            };
        }
    }
}
