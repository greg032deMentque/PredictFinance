using BackPredictFinance.Common;
using BackPredictFinance.Common.enums;
using BackPredictFinance.Services.PythonServices;
using BackPredictFinance.ViewModels.ClientFinanceViewModels.AnalysisV1;
using System.Net;

namespace BackPredictFinance.Services.ClientFinanceServices.AnalysisV1
{
    public sealed class ClientAnalysisOrchestrator : IAnalysisOrchestrator
    {
        private readonly IAnalysisPatternRegistry _patternRegistry;
        private readonly IOptionalPythonAnalysisAdapter _pythonAnalysisAdapter;
        private readonly IRecommendationPolicyService _recommendationPolicyService;
        private readonly IAnalysisSnapshotPersistenceService _snapshotPersistenceService;

        public ClientAnalysisOrchestrator(
            IAnalysisPatternRegistry patternRegistry,
            IOptionalPythonAnalysisAdapter pythonAnalysisAdapter,
            IRecommendationPolicyService recommendationPolicyService,
            IAnalysisSnapshotPersistenceService snapshotPersistenceService)
        {
            _patternRegistry = patternRegistry;
            _pythonAnalysisAdapter = pythonAnalysisAdapter;
            _recommendationPolicyService = recommendationPolicyService;
            _snapshotPersistenceService = snapshotPersistenceService;
        }

        public async Task<AnalysisResponse> RunAnalysisAsync(ResolvedAnalysisRunRequest request, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(request);

            var startedAtUtc = DateTime.UtcNow;
            var resolvedPattern = _patternRegistry.ResolveRequestedPattern(request.RequestedPatternId);

            AnalysisExecutionArtifact executionArtifact;
            try
            {
                executionArtifact = await _pythonAnalysisAdapter.ExecuteAsync(request, resolvedPattern, ct);
            }
            catch (Exception ex)
            {
                var failedAtUtc = DateTime.UtcNow;
                await _snapshotPersistenceService.PersistFailedAnalysisAsync(request, resolvedPattern, startedAtUtc, failedAtUtc, ex, ct);

                if (ex is CustomException customException)
                {
                    throw customException;
                }

                var envelope = PythonCliErrorHandling.GetOrBuildEnvelope(ex, "predict", request.Symbol, resolvedPattern.PatternId);
                throw PythonCliErrorHandling.CreateCustomException(
                    "predict",
                    request.Symbol,
                    resolvedPattern.PatternId,
                    envelope,
                    overrideStatusCode: HttpStatusCode.InternalServerError,
                    overrideFrontMessage: "Le moteur IA n'a pas pu terminer l'analyse.");
            }

            var recommendation = _recommendationPolicyService.EvaluateAnalysis(request, executionArtifact);
            var completedAtUtc = DateTime.UtcNow;
            var persisted = await _snapshotPersistenceService.PersistSuccessfulAnalysisAsync(
                request,
                resolvedPattern,
                executionArtifact,
                recommendation,
                startedAtUtc,
                completedAtUtc,
                ct);

            return BuildAnalysisResponse(request, persisted, resolvedPattern, executionArtifact, recommendation);
        }

        private static AnalysisResponse BuildAnalysisResponse(
            ResolvedAnalysisRunRequest request,
            PersistedAnalysisRecord persisted,
            ResolvedAnalysisPattern resolvedPattern,
            AnalysisExecutionArtifact executionArtifact,
            Recommendation recommendation)
        {
            var orderedPatterns = executionArtifact.Patterns
                .OrderByDescending(pattern => pattern.IsPrimary)
                .ThenByDescending(pattern => pattern.Confidence)
                .ThenByDescending(pattern => pattern.Probability)
                .ToList();

            var compatiblePatterns = orderedPatterns
                .Select(pattern => pattern.ContractAssessment)
                .Where(pattern => pattern.Detection.IsCompatible)
                .ToList();

            var mainPattern = compatiblePatterns.FirstOrDefault();
            var alternativePatterns = compatiblePatterns.Skip(1).ToList();
            var outcome = compatiblePatterns.Count switch
            {
                0 => AnalysisOutcome.NoCrediblePattern,
                > 1 => AnalysisOutcome.MultipleCompatiblePatterns,
                _ => AnalysisOutcome.CrediblePatternFound
            };

            return new AnalysisResponse
            {
                AnalysisId = persisted.PublicId,
                GeneratedAtUtc = executionArtifact.GeneratedAtUtc,
                AsOfDate = DateOnly.FromDateTime(executionArtifact.GeneratedAtUtc),
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
                RequestedPatternIds = string.IsNullOrWhiteSpace(request.RequestedPatternId) ? [] : [request.RequestedPatternId],
                ExecutedPatternIds = [resolvedPattern.PatternId],
                MainPattern = mainPattern,
                AlternativePatterns = alternativePatterns,
                Recommendation = recommendation,
                PedagogicalSummary = recommendation.Rationale,
                NoCrediblePatternReason = outcome == AnalysisOutcome.NoCrediblePattern
                    ? "Aucun pattern compatible n'a ete identifie."
                    : null,
                Trace = new AnalysisResponseTrace
                {
                    TraceId = persisted.PublicId,
                    AnalysisEngineVersion = executionArtifact.ModelVersion,
                    RuleSetVersion = resolvedPattern.ModelVersion
                },
                Warnings = executionArtifact.ModelStatus == ModelStatusEnum.Go
                    ? []
                    : [string.IsNullOrWhiteSpace(executionArtifact.ModelMessage) ? "Le modele legacy n'est pas pleinement valide." : executionArtifact.ModelMessage]
            };
        }
    }
}
