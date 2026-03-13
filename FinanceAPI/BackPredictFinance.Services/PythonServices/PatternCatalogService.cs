using Microsoft.Extensions.Options;

namespace BackPredictFinance.Services.PythonServices
{
    public interface IPatternCatalogService
    {
        PatternCatalogItem Resolve(string? patternKey = null);
        IReadOnlyList<PatternCatalogItem> GetAll();
    }

    public sealed class PatternCatalogService : IPatternCatalogService
    {
        private readonly PythonCliOptions _options;

        public PatternCatalogService(IOptions<PythonCliOptions> options)
        {
            _options = options.Value;
        }

        public PatternCatalogItem Resolve(string? patternKey = null)
        {
            var normalizedPattern = NormalizePatternKey(patternKey);
            if (_options.Patterns.TryGetValue(normalizedPattern, out var configuredPattern))
            {
                if (!configuredPattern.Enabled)
                {
                    throw new InvalidOperationException($"Pattern '{normalizedPattern}' is disabled.");
                }

                return new PatternCatalogItem
                {
                    PatternKey = normalizedPattern,
                    Enabled = true,
                    ModelDir = string.IsNullOrWhiteSpace(configuredPattern.ModelDir) ? _options.ModelDir : configuredPattern.ModelDir.Trim(),
                    ModelVersion = string.IsNullOrWhiteSpace(configuredPattern.ModelVersion) ? _options.ModelVersion : configuredPattern.ModelVersion.Trim()
                };
            }

            if (normalizedPattern == NormalizePatternKey(_options.DefaultPattern))
            {
                return new PatternCatalogItem
                {
                    PatternKey = normalizedPattern,
                    Enabled = true,
                    ModelDir = _options.ModelDir,
                    ModelVersion = _options.ModelVersion
                };
            }

            throw new InvalidOperationException($"Pattern '{normalizedPattern}' is not configured in the API pattern catalog.");
        }

        public IReadOnlyList<PatternCatalogItem> GetAll()
        {
            if (_options.Patterns.Count == 0)
            {
                return [Resolve(_options.DefaultPattern)];
            }

            return _options.Patterns
                .Select(entry => new PatternCatalogItem
                {
                    PatternKey = NormalizePatternKey(entry.Key),
                    Enabled = entry.Value.Enabled,
                    ModelDir = string.IsNullOrWhiteSpace(entry.Value.ModelDir) ? _options.ModelDir : entry.Value.ModelDir.Trim(),
                    ModelVersion = string.IsNullOrWhiteSpace(entry.Value.ModelVersion) ? _options.ModelVersion : entry.Value.ModelVersion.Trim()
                })
                .OrderBy(entry => entry.PatternKey, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private string NormalizePatternKey(string? patternKey)
        {
            var normalized = (patternKey ?? string.Empty).Trim().ToUpperInvariant();
            if (!string.IsNullOrWhiteSpace(normalized))
            {
                return normalized;
            }

            var defaultPattern = (_options.DefaultPattern ?? string.Empty).Trim().ToUpperInvariant();
            return string.IsNullOrWhiteSpace(defaultPattern) ? "DOUBLE_TOP" : defaultPattern;
        }
    }

    public sealed class PatternCatalogItem
    {
        public string PatternKey { get; set; } = string.Empty;
        public bool Enabled { get; set; }
        public string ModelDir { get; set; } = string.Empty;
        public string ModelVersion { get; set; } = string.Empty;
    }
}
