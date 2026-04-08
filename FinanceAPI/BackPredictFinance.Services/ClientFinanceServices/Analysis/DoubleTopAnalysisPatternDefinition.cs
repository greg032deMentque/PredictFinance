using BackPredictFinance.Contracts.Analysis;
using BackPredictFinance.Services.TwelveDataServices;

namespace BackPredictFinance.Services.ClientFinanceServices.Analysis
{
    public sealed class DoubleTopAnalysisPatternDefinition : IAnalysisPatternDefinition
    {
        public DoubleTopAnalysisPatternDefinition(ITickerService tickerService)
        {
        }

        public string PatternId => "DOUBLE_TOP";
        public string DisplayName => "Double sommet";
        public string FamilyId => "REVERSAL";
        public string BiasCode => "BEARISH";
        public string ModelVersion => "analysis-v1-deterministic-double-top@legacy";
        public int HistoryLookbackMonths => 6;
        public int MinimumRequiredCandles => 47;

        public ResolvedAnalysisPattern BuildResolvedPattern()
        {
            return new ResolvedAnalysisPattern
            {
                PatternId = PatternId,
                DisplayName = DisplayName,
                FamilyId = FamilyId,
                BiasCode = BiasCode,
                ModelVersion = ModelVersion,
                HistoryLookbackMonths = HistoryLookbackMonths,
                MinimumRequiredCandles = MinimumRequiredCandles
            };
        }

        public Task<AnalysisExecutionArtifact> ExecuteAsync(AnalysisRequest request, CancellationToken ct = default)
        {
            throw new InvalidOperationException("Le pattern DOUBLE_TOP n'appartient plus au scope actif du runtime V1 de continuation.");
        }
    }
}
