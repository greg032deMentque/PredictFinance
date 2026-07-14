using BackPredictFinance.Common.AnalysisV1;
using BackPredictFinance.Common.enums;
using BackPredictFinance.Patterns;
using BackPredictFinance.Patterns.Contracts;

namespace BackPredictFinance.Services.ClientFinanceServices.Analysis
{
    /// <summary>
    /// Exécute le moteur déterministe de détection de patterns.
    /// </summary>
    public interface IAnalysisExecutionService
    {
        /// <summary>
        /// Exécute les patterns résolus pour produire un artefact d'analyse fusionné.
        /// </summary>
        Task<AnalysisExecutionArtifact> ExecuteAsync(AnalysisRequest request, CancellationToken ct = default);
    }

    /// <summary>
    /// Implémente l'exécution déterministe des patterns activés.
    /// </summary>
    public sealed class DeterministicAnalysisExecutionService : IAnalysisExecutionService
    {
        private readonly IAnalysisPatternRegistry _patternRegistry;

        public DeterministicAnalysisExecutionService(IAnalysisPatternRegistry patternRegistry)
        {
            _patternRegistry = patternRegistry;
        }

        public async Task<AnalysisExecutionArtifact> ExecuteAsync(AnalysisRequest request, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(request);

            var definitions = _patternRegistry.ResolveDefinitions(request.ResolvedPatternIds);
            if (definitions.Count == 0)
            {
                throw new InvalidOperationException("Aucun pattern actif n'a ete resolu pour l'execution V1.");
            }

            var executionArtifacts = new List<AnalysisExecutionArtifact>(definitions.Count);
            foreach (var definition in definitions)
            {
                executionArtifacts.Add(await definition.ExecuteAsync(request, ct));
            }

            return MergeExecutionArtifacts(request, executionArtifacts);
        }

        private static AnalysisExecutionArtifact MergeExecutionArtifacts(AnalysisRequest request, IReadOnlyList<AnalysisExecutionArtifact> executionArtifacts)
        {
            ArgumentNullException.ThrowIfNull(request);
            ArgumentNullException.ThrowIfNull(executionArtifacts);

            if (executionArtifacts.Count == 0)
            {
                throw new InvalidOperationException("Aucun artefact d'execution n'a ete produit.");
            }

            var orderedPatterns = executionArtifacts
                .SelectMany(artifact => artifact.Patterns)
                .OrderByDescending(pattern => pattern.IsPrimary)
                .ThenByDescending(pattern => pattern.Confidence)
                .ThenByDescending(pattern => pattern.Probability)
                .ToList();

            var generatedAtUtc = executionArtifacts
                .Select(artifact => artifact.GeneratedAtUtc)
                .DefaultIfEmpty(DateTime.UtcNow)
                .Max();

            var modelStatus = executionArtifacts.Any(artifact => artifact.ModelStatus == ModelStatusEnum.Go)
                ? ModelStatusEnum.Go
                : executionArtifacts[0].ModelStatus;

            var modelMessage = string.Join(
                " | ",
                executionArtifacts
                    .Select(artifact => artifact.ModelMessage)
                    .Where(message => !string.IsNullOrWhiteSpace(message))
                    .Select(message => message.Trim())
                    .Distinct(StringComparer.OrdinalIgnoreCase));

            var modelVersion = string.Join(
                ";",
                executionArtifacts
                    .Select(artifact => string.IsNullOrWhiteSpace(artifact.ModelVersion) ? null : artifact.ModelVersion.Trim())
                    .Where(version => !string.IsNullOrWhiteSpace(version))
                    .Distinct(StringComparer.OrdinalIgnoreCase)!);

            var rawProviderPayloadJson = executionArtifacts.Count == 1
                ? executionArtifacts[0].RawProviderPayloadJson
                : System.Text.Json.JsonSerializer.Serialize(new
                {
                    request.Instrument.Symbol,
                    request.ResolvedPatternIds,
                    artifacts = executionArtifacts.Select(artifact => new
                    {
                        artifact.Symbol,
                        artifact.GeneratedAtUtc,
                        artifact.ModelVersion,
                        artifact.RawProviderPayloadJson
                    })
                });

            var candles = executionArtifacts
                .Where(artifact => artifact.Candles.Count > 0)
                .OrderByDescending(artifact => artifact.Candles.Count)
                .Select(artifact => artifact.Candles)
                .FirstOrDefault()
                ?? [];

            return new AnalysisExecutionArtifact
            {
                Symbol = request.Instrument.Symbol,
                GeneratedAtUtc = generatedAtUtc,
                Patterns = orderedPatterns,
                ModelStatus = modelStatus,
                ModelMessage = modelMessage,
                ModelVersion = modelVersion,
                Precision = executionArtifacts.Select(artifact => artifact.Precision).FirstOrDefault(value => value.HasValue),
                F1 = executionArtifacts.Select(artifact => artifact.F1).FirstOrDefault(value => value.HasValue),
                RocAuc = executionArtifacts.Select(artifact => artifact.RocAuc).FirstOrDefault(value => value.HasValue),
                PositiveSamples = executionArtifacts.Select(artifact => artifact.PositiveSamples).FirstOrDefault(value => value.HasValue),
                SelectedThreshold = executionArtifacts.Select(artifact => artifact.SelectedThreshold).FirstOrDefault(value => value.HasValue),
                RawProviderPayloadJson = rawProviderPayloadJson,
                Candles = candles
            };
        }
    }
}
