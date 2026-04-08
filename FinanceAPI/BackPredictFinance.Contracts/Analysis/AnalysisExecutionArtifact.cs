using BackPredictFinance.Contracts.Analysis;
using BackPredictFinance.Common.enums;

namespace BackPredictFinance.Contracts.Analysis
{
    public sealed class AnalysisExecutionArtifact
    {
        public string Symbol { get; set; } = string.Empty;
        public DateTime GeneratedAtUtc { get; set; }
        public List<ExecutedPatternArtifact> Patterns { get; set; } = [];
        public ModelStatusEnum ModelStatus { get; set; } = ModelStatusEnum.NoGo;
        public string ModelMessage { get; set; } = string.Empty;
        public string ModelVersion { get; set; } = string.Empty;
        public decimal? Precision { get; set; }
        public decimal? F1 { get; set; }
        public decimal? RocAuc { get; set; }
        public int? PositiveSamples { get; set; }
        public decimal? SelectedThreshold { get; set; }
        public string RawProviderPayloadJson { get; set; } = string.Empty;

        public IReadOnlyList<ExecutedPatternArtifact> GetOrderedPatterns()
        {
            return Patterns
                .OrderByDescending(pattern => pattern.IsPrimary)
                .ThenByDescending(pattern => pattern.Confidence)
                .ThenByDescending(pattern => pattern.Probability)
                .ToList();
        }

        public IReadOnlyList<PatternAssessment> GetCompatiblePatternAssessments()
        {
            return GetOrderedPatterns()
                .Select(pattern => pattern.ContractAssessment)
                .Where(pattern => pattern.Detection.IsCompatible)
                .ToList();
        }

        public List<string> GetExecutedPatternIds(string fallbackPatternId)
        {
            var executedPatternIds = GetOrderedPatterns()
                .Select(pattern => pattern.ContractAssessment.PatternId)
                .Where(patternId => !string.IsNullOrWhiteSpace(patternId))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (executedPatternIds.Count == 0 && !string.IsNullOrWhiteSpace(fallbackPatternId))
            {
                executedPatternIds.Add(fallbackPatternId.Trim());
            }

            return executedPatternIds;
        }

        public string ResolveRuleSetVersion(string fallbackRuleSetVersion)
        {
            var resolvedRuleSetVersion = GetOrderedPatterns()
                .Select(pattern => pattern.ContractAssessment.Trace.RuleSetVersion)
                .FirstOrDefault(ruleSetVersion => !string.IsNullOrWhiteSpace(ruleSetVersion));

            return string.IsNullOrWhiteSpace(resolvedRuleSetVersion)
                ? fallbackRuleSetVersion
                : resolvedRuleSetVersion.Trim();
        }

        public string ResolveAnalysisEngineVersion(string fallbackAnalysisEngineVersion)
        {
            return string.IsNullOrWhiteSpace(ModelVersion)
                ? fallbackAnalysisEngineVersion
                : ModelVersion.Trim();
        }
    }
}
