using BackPredictFinance.Contracts.Analysis;
using BackPredictFinance.ViewModels.ClientFinanceViewModels.AnalysisV1;

namespace BackPredictFinance.Services.ClientFinanceServices.Analysis
{
    public sealed class DeterministicAnalysisExecutionService : IAnalysisExecutionService
    {
        private readonly IAnalysisPatternRegistry _patternRegistry;

        public DeterministicAnalysisExecutionService(IAnalysisPatternRegistry patternRegistry)
        {
            _patternRegistry = patternRegistry;
        }

        public Task<AnalysisExecutionArtifact> ExecuteAsync(AnalysisRequest request, ResolvedAnalysisPattern pattern, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(request);
            ArgumentNullException.ThrowIfNull(pattern);

            var definition = _patternRegistry.ResolveDefinition(pattern.PatternId);
            return definition.ExecuteAsync(request, ct);
        }
    }
}
