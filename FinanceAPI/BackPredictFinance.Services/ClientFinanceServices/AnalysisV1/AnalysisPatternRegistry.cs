using BackPredictFinance.Services.PythonServices;

namespace BackPredictFinance.Services.ClientFinanceServices.AnalysisV1
{
    public sealed class AnalysisPatternRegistry : IAnalysisPatternRegistry
    {
        private readonly IPatternCatalogService _patternCatalogService;

        public AnalysisPatternRegistry(IPatternCatalogService patternCatalogService)
        {
            _patternCatalogService = patternCatalogService;
        }

        public ResolvedAnalysisPattern ResolveRequestedPattern(string? requestedPattern)
        {
            var resolved = _patternCatalogService.Resolve(requestedPattern);
            return new ResolvedAnalysisPattern
            {
                PatternId = resolved.PatternKey,
                ModelDir = resolved.ModelDir,
                ModelVersion = resolved.ModelVersion
            };
        }

        public IReadOnlyList<ResolvedAnalysisPattern> GetEnabledPatterns()
        {
            return _patternCatalogService.GetAll()
                .Where(pattern => pattern.Enabled)
                .Select(pattern => new ResolvedAnalysisPattern
                {
                    PatternId = pattern.PatternKey,
                    ModelDir = pattern.ModelDir,
                    ModelVersion = pattern.ModelVersion
                })
                .ToList();
        }
    }
}
