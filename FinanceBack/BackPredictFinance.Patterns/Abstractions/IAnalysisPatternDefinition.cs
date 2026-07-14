using BackPredictFinance.Common.AnalysisV1;
using BackPredictFinance.Patterns.Contracts;

namespace BackPredictFinance.Patterns.Abstractions
{
    public interface IAnalysisPatternDefinition
    {
        string PatternId { get; }
        string ModelVersion { get; }
        int HistoryLookbackMonths { get; }
        ResolvedAnalysisPattern BuildResolvedPattern();
        Task<AnalysisExecutionArtifact> ExecuteAsync(AnalysisRequest request, CancellationToken ct = default);
    }
}
