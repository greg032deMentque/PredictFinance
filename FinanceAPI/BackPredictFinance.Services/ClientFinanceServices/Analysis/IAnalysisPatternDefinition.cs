using BackPredictFinance.Contracts.Analysis;

namespace BackPredictFinance.Services.ClientFinanceServices.Analysis
{
    public interface IAnalysisPatternDefinition
    {
        string PatternId { get; }
        string DisplayName { get; }
        string FamilyId { get; }
        string BiasCode { get; }
        string ModelVersion { get; }
        int HistoryLookbackMonths { get; }
        int MinimumRequiredCandles { get; }
        ResolvedAnalysisPattern BuildResolvedPattern();
        Task<AnalysisExecutionArtifact> ExecuteAsync(AnalysisRequest request, CancellationToken ct = default);
    }
}
