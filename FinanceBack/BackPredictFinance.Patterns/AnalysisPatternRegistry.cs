using BackPredictFinance.Patterns.Abstractions;
using BackPredictFinance.Patterns.Contracts;

namespace BackPredictFinance.Patterns
{
    public interface IAnalysisPatternRegistry
    {
        ResolvedAnalysisPattern ResolveRequestedPattern(string? requestedPatternId);
        IReadOnlyList<ResolvedAnalysisPattern> ResolveRequestedPatterns(IEnumerable<string>? requestedPatternIds);
        IAnalysisPatternDefinition ResolveDefinition(string? requestedPatternId);
        IReadOnlyList<IAnalysisPatternDefinition> ResolveDefinitions(IEnumerable<string>? requestedPatternIds);
        IReadOnlyList<ResolvedAnalysisPattern> GetEnabledPatterns();
    }

    /// <summary>
    /// Point d'entree unique pour resoudre un identifiant de pattern (venu de l'API ou d'une
    /// configuration) vers l'implementation <see cref="IAnalysisPatternDefinition"/> injectee par
    /// DI correspondante. Centralise aussi la liste des patterns "actifs" pour l'explorateur
    /// (<see cref="GetEnabledPatterns"/>), definie par <see cref="PatternCatalog"/>.
    /// </summary>
    public sealed class AnalysisPatternRegistry : IAnalysisPatternRegistry
    {
        private readonly IReadOnlyDictionary<string, IAnalysisPatternDefinition> _definitions;

        public AnalysisPatternRegistry(IEnumerable<IAnalysisPatternDefinition> definitions)
        {
            ArgumentNullException.ThrowIfNull(definitions);

            // group.Single() : deux definitions enregistrees avec le meme PatternId (normalise)
            // sont une erreur de configuration DI et doivent lever plutot que de se substituer
            // silencieusement l'une a l'autre.
            _definitions = definitions
                .GroupBy(definition => NormalizePatternId(definition.PatternId), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    group => group.Key,
                    group => group.Single(),
                    StringComparer.OrdinalIgnoreCase);

            if (_definitions.Count == 0)
            {
                throw new InvalidOperationException("Aucun pattern d'analyse V1 actif n'est enregistre dans l'API.");
            }
        }

        public ResolvedAnalysisPattern ResolveRequestedPattern(string? requestedPatternId)
        {
            return ResolveDefinition(requestedPatternId).BuildResolvedPattern();
        }

        public IReadOnlyList<ResolvedAnalysisPattern> ResolveRequestedPatterns(IEnumerable<string>? requestedPatternIds)
        {
            return ResolveDefinitions(requestedPatternIds)
                .Select(definition => definition.BuildResolvedPattern())
                .ToList();
        }

        public IAnalysisPatternDefinition ResolveDefinition(string? requestedPatternId)
        {
            var normalizedPatternId = NormalizePatternId(requestedPatternId);
            if (string.IsNullOrWhiteSpace(normalizedPatternId))
            {
                throw new InvalidOperationException("Un identifiant de pattern explicite est obligatoire pour resoudre une definition unique.");
            }

            if (_definitions.TryGetValue(normalizedPatternId, out var definition))
            {
                return definition;
            }

            throw new InvalidOperationException($"Le runtime V1 actif ne prend pas en charge le pattern {normalizedPatternId}.");
        }

        public IReadOnlyList<IAnalysisPatternDefinition> ResolveDefinitions(IEnumerable<string>? requestedPatternIds)
        {
            var normalizedPatternIds = (requestedPatternIds ?? [])
                .Select(NormalizePatternId)
                .Where(patternId => !string.IsNullOrWhiteSpace(patternId))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (normalizedPatternIds.Count == 0)
            {
                var targetPatternIds = PatternCatalog.GetTargetPatterns()
                    .Select(pattern => NormalizePatternId(pattern.PatternId))
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);

                return _definitions.Values
                    .Where(definition => targetPatternIds.Contains(NormalizePatternId(definition.PatternId)))
                    .OrderBy(definition => definition.PatternId, StringComparer.OrdinalIgnoreCase)
                    .ToList();
            }

            return normalizedPatternIds
                .Select(ResolveDefinition)
                .ToList();
        }

        public IReadOnlyList<ResolvedAnalysisPattern> GetEnabledPatterns()
        {
            var targetPatternIds = PatternCatalog.GetTargetPatterns()
                .Select(pattern => NormalizePatternId(pattern.PatternId))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            return _definitions.Values
                .Where(definition => targetPatternIds.Contains(NormalizePatternId(definition.PatternId)))
                .OrderBy(definition => definition.PatternId, StringComparer.OrdinalIgnoreCase)
                .Select(definition => definition.BuildResolvedPattern())
                .ToList();
        }

        private static string NormalizePatternId(string? patternId)
        {
            return (patternId ?? string.Empty).Trim().ToUpperInvariant();
        }
    }
}
