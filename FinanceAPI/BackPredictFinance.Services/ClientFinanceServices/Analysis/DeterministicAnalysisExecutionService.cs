using BackPredictFinance.Common.enums;
using BackPredictFinance.Contracts.Analysis;

namespace BackPredictFinance.Services.ClientFinanceServices.Analysis
{
    public interface IAnalysisExecutionService
    {
        Task<AnalysisExecutionArtifact> ExecuteAsync(AnalysisRequest request, IReadOnlyList<ResolvedAnalysisPattern> patterns, CancellationToken ct = default);
    }

    public sealed class DeterministicAnalysisExecutionService : IAnalysisExecutionService
    {
        private readonly IAnalysisPatternRegistry _patternRegistry;

        public DeterministicAnalysisExecutionService(IAnalysisPatternRegistry patternRegistry)
        {
            _patternRegistry = patternRegistry;
        }

        public async Task<AnalysisExecutionArtifact> ExecuteAsync(AnalysisRequest request, IReadOnlyList<ResolvedAnalysisPattern> patterns, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(request);
            ArgumentNullException.ThrowIfNull(patterns);

            if (patterns.Count == 0)
            {
                throw new InvalidOperationException("Aucun pattern actif n'a ete resolu pour l'analyse.");
            }

            var artifacts = new List<AnalysisExecutionArtifact>(patterns.Count);
            foreach (var pattern in patterns)
            {
                var definition = _patternRegistry.ResolveDefinition(pattern.PatternId);
                artifacts.Add(await definition.ExecuteAsync(request, ct));
            }

            var orderedArtifacts = artifacts.OrderByDescending(x => x.GeneratedAtUtc).ToList();
            var mergedPatterns = orderedArtifacts.SelectMany(x => x.Patterns).ToList();
            var primaryCompatiblePatternId = mergedPatterns
                .Where(x => x.ContractAssessment.Detection.IsCompatible)
                .OrderByDescending(x => x.ContractAssessment.Scoring.ConfidenceScore)
                .Select(x => x.ContractAssessment.PatternId)
                .FirstOrDefault();

            foreach (var pattern in mergedPatterns)
            {
                pattern.IsPrimary = string.Equals(pattern.ContractAssessment.PatternId, primaryCompatiblePatternId, StringComparison.OrdinalIgnoreCase);
                pattern.ContractAssessment.Trace.IsPrimaryDisplayCandidate = pattern.IsPrimary;
            }

            return new AnalysisExecutionArtifact
            {
                Symbol = orderedArtifacts[0].Symbol,
                GeneratedAtUtc = orderedArtifacts.Max(x => x.GeneratedAtUtc),
                Patterns = mergedPatterns,
                ModelStatus = orderedArtifacts.All(x => x.ModelStatus == ModelStatusEnum.Go) ? ModelStatusEnum.Go : ModelStatusEnum.NoGo,
                ModelMessage = string.Join(" | ", orderedArtifacts.Select(x => x.ModelMessage).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.Ordinal)),
                ModelVersion = string.Join(";", patterns.Select(x => x.ModelVersion).Distinct(StringComparer.OrdinalIgnoreCase)),
                RawProviderPayloadJson = orderedArtifacts[0].RawProviderPayloadJson
            };
        }
    }
}
