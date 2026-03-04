using BackPredictFinance.Common.enums;

namespace BackPredictFinance.Services.PythonServices.Models
{
    public sealed class IAHealthResponse
    {
        public IAHealthStatusEnum Status { get; set; }
        public DateTime CheckedAtUtc { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public sealed class IAStatusResponse
    {
        public IAHealthStatusEnum Status { get; set; }
        public DateTime CheckedAtUtc { get; set; }
        public bool PredictCliAvailable { get; set; }
        public PythonRuntimeInfo Python { get; set; } = new();
        public DirectoryStatusInfo WorkingDirectory { get; set; } = new();
        public ModelArtifactsInfo ModelArtifacts { get; set; } = new();
        public QualityGateThresholds QualityGate { get; set; } = new();
        public List<string> Notes { get; set; } = [];
    }

    public sealed class PythonRuntimeInfo
    {
        public string ExecutablePath { get; set; } = string.Empty;
        public bool ExecutableExists { get; set; }
        public string Version { get; set; } = string.Empty;
    }

    public sealed class DirectoryStatusInfo
    {
        public string Path { get; set; } = string.Empty;
        public bool Exists { get; set; }
    }

    public sealed class ModelArtifactsInfo
    {
        public string ModelDirectoryPath { get; set; } = string.Empty;
        public bool DirectoryExists { get; set; }
        public bool ModelFileExists { get; set; }
        public bool MetricsFileExists { get; set; }
        public DateTime? MetricsLastWriteUtc { get; set; }
        public ModelMetricsSummary Metrics { get; set; } = new();
    }

    public sealed class ModelMetricsSummary
    {
        public decimal? Precision { get; set; }
        public decimal? F1 { get; set; }
        public decimal? RocAuc { get; set; }
        public decimal? SelectedThreshold { get; set; }
    }

    public sealed class QualityGateThresholds
    {
        public decimal MinPrecision { get; set; }
        public decimal MinF1 { get; set; }
        public decimal MinRocAuc { get; set; }
        public int MinPositives { get; set; }
    }
}
