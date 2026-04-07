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
    }
}
