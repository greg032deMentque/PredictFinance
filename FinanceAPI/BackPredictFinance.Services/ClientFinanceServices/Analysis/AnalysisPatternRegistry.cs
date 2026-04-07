using BackPredictFinance.Contracts.Analysis;
namespace BackPredictFinance.Services.ClientFinanceServices.Analysis
{

public interface IAnalysisPatternRegistry
{
    ResolvedAnalysisPattern ResolveRequestedPattern(string? requestedPattern);
    IAnalysisPatternDefinition ResolveDefinition(string? requestedPattern);
    IReadOnlyList<ResolvedAnalysisPattern> GetEnabledPatterns();
}


    public sealed class AnalysisPatternRegistry : IAnalysisPatternRegistry
    {
        private readonly IReadOnlyDictionary<string, IAnalysisPatternDefinition> _definitions;

        public AnalysisPatternRegistry(IEnumerable<IAnalysisPatternDefinition> definitions)
        {
            ArgumentNullException.ThrowIfNull(definitions);

            _definitions = definitions
                .GroupBy(definition => definition.PatternId, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    group => group.Key,
                    group => group.Single(),
                    StringComparer.OrdinalIgnoreCase);

            if (_definitions.Count == 0)
            {
                throw new InvalidOperationException("Aucun pattern d'analyse V1 actif n'est enregistre dans l'API.");
            }
        }

        public ResolvedAnalysisPattern ResolveRequestedPattern(string? requestedPattern)
        {
            return ResolveDefinition(requestedPattern).BuildResolvedPattern();
        }

        public IAnalysisPatternDefinition ResolveDefinition(string? requestedPattern)
        {
            var normalizedPattern = (requestedPattern ?? string.Empty).Trim().ToUpperInvariant();
            if (string.IsNullOrWhiteSpace(normalizedPattern))
            {
                return GetSingleEnabledDefinition();
            }

            if (_definitions.TryGetValue(normalizedPattern, out var definition))
            {
                return definition;
            }

            throw new InvalidOperationException($"Le runtime V1 actif ne prend pas en charge le pattern {normalizedPattern}.");
        }

        public IReadOnlyList<ResolvedAnalysisPattern> GetEnabledPatterns()
        {
            return _definitions.Values
                .Select(definition => definition.BuildResolvedPattern())
                .OrderBy(definition => definition.PatternId, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private IAnalysisPatternDefinition GetSingleEnabledDefinition()
        {
            if (_definitions.Count != 1)
            {
                throw new InvalidOperationException("Le runtime V1 actif requiert un pattern explicite tant que plusieurs definitions sont enregistrees.");
            }

            return _definitions.Values.Single();
        }
    }
}
