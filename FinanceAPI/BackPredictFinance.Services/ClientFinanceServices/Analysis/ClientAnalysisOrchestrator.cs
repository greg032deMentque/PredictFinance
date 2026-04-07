using BackPredictFinance.Common;
using BackPredictFinance.Common.AnalysisV1;
using BackPredictFinance.Common.enums;
using BackPredictFinance.Contracts.Analysis;
using BackPredictFinance.ViewModels.ClientFinanceViewModels;
using BackPredictFinance.ViewModels.ClientFinanceViewModels.AnalysisV1;
using System.Net;

namespace BackPredictFinance.Services.ClientFinanceServices.Analysis
{

    public interface IAnalysisOrchestrator
    {
        Task<AnalysisResponseViewModel> RunAnalysisAsync(AnalysisRequest request, CancellationToken ct = default);
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

        public async Task<AnalysisResponseViewModel> RunAnalysisAsync(AnalysisRequest request, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(request);

            var startedAtUtc = DateTime.UtcNow;
            var resolvedPatternId = request.ResolvedPatternIds.FirstOrDefault();
            var resolvedPattern = _patternRegistry.ResolveRequestedPattern(resolvedPatternId);

            AnalysisExecutionArtifact executionArtifact;
            try
            {
                executionArtifact = await _analysisExecutionService.ExecuteAsync(request, resolvedPattern, ct);
            }
            catch (Exception ex)
            {
                var failedAtUtc = DateTime.UtcNow;
                await _snapshotPersistenceService.PersistFailedAnalysisAsync(request, resolvedPattern, startedAtUtc, failedAtUtc, ex, ct);

                if (ex is CustomException customException)
                {
                    throw customException;
                }

                throw new CustomException(
                    $"L'analyse V1 a echoue pour {request.Instrument.Symbol} sur le pattern {resolvedPattern.PatternId}: {ex.Message}",
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
                resolvedPattern,
                executionArtifact,
                recommendation,
                outcome,
                pedagogicalSummary,
                _pedagogicalExplanationService.PolicyVersion,
                startedAtUtc,
                completedAtUtc,
                ct);

            return BuildAnalysisResponse(request, persisted, resolvedPattern, executionArtifact, recommendation, outcome, pedagogicalSummary);
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
                    : [string.IsNullOrWhiteSpace(executionArtifact.ModelMessage) ? "Le modele legacy n'est pas pleinement valide." : executionArtifact.ModelMessage],
                ModelStatus = executionArtifact.ModelStatus,
                ModelMessage = string.IsNullOrWhiteSpace(executionArtifact.ModelMessage) ? string.Empty : executionArtifact.ModelMessage.Trim()
            };
        }
    }
}
