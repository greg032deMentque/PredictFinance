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
                .GroupBy(definition => PatternIds.Normalize(definition.PatternId), StringComparer.OrdinalIgnoreCase)
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
            var normalizedPatternId = PatternIds.Normalize(requestedPatternId);
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
                .Select(PatternIds.Normalize)
                .Where(patternId => !string.IsNullOrWhiteSpace(patternId))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (normalizedPatternIds.Count == 0)
            {
                return ResolveEnabledDefinitions();
            }

            return normalizedPatternIds
                .Select(ResolveDefinition)
                .ToList();
        }

        public IReadOnlyList<ResolvedAnalysisPattern> GetEnabledPatterns()
        {
            return ResolveEnabledDefinitions()
                .Select(definition => definition.BuildResolvedPattern())
                .ToList();
        }

        // Facteur commun a ResolveDefinitions (fallback liste vide) et GetEnabledPatterns : ne
        // garde que les definitions enregistrees dont le PatternId figure dans le catalogue des
        // patterns actifs (PatternCatalog), triees par PatternId.
        private IReadOnlyList<IAnalysisPatternDefinition> ResolveEnabledDefinitions()
        {
            var targetPatternIds = PatternCatalog.GetTargetPatterns()
                .Select(pattern => PatternIds.Normalize(pattern.PatternId))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            return _definitions.Values
                .Where(definition => targetPatternIds.Contains(PatternIds.Normalize(definition.PatternId)))
                .OrderBy(definition => definition.PatternId, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
    }
}
