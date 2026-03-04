using BackPredictFinance.Common.enums;
using BackPredictFinance.Services.PythonServices.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using System.Diagnostics;
using System.Text.Json;

namespace BackPredictFinance.Services.PythonServices
{
    public interface IIAStatusService
    {
        Task<IAHealthResponse> GetHealthAsync(CancellationToken ct = default);
        Task<IAStatusResponse> GetStatusAsync(CancellationToken ct = default);
    }

    public sealed class IAStatusService : IIAStatusService
    {
        private readonly PythonCliOptions _options;
        private readonly IHostEnvironment _environment;
        private readonly IPythonApiService _pythonApiService;

        public IAStatusService(
            IOptions<PythonCliOptions> options,
            IHostEnvironment environment,
            IPythonApiService pythonApiService)
        {
            _options = options.Value;
            _environment = environment;
            _pythonApiService = pythonApiService;
        }

        public async Task<IAHealthResponse> GetHealthAsync(CancellationToken ct = default)
        {
            var status = await GetStatusAsync(ct);
            return new IAHealthResponse
            {
                Status = status.Status,
                CheckedAtUtc = status.CheckedAtUtc,
                Message = BuildHealthMessage(status)
            };
        }

        public async Task<IAStatusResponse> GetStatusAsync(CancellationToken ct = default)
        {
            var checkedAtUtc = DateTime.UtcNow;
            var notes = new List<string>();

            var pythonPath = ResolvePath(_options.PythonExe);
            var workingDirectoryPath = ResolvePath(_options.WorkingDirectory);
            var modelDirectoryPath = ResolveModelDirectoryPath(workingDirectoryPath);
            var modelFilePath = Path.Combine(modelDirectoryPath, "model.joblib");
            var metricsFilePath = Path.Combine(modelDirectoryPath, "metrics.json");

            var pythonInfo = new PythonRuntimeInfo
            {
                ExecutablePath = pythonPath,
                ExecutableExists = File.Exists(pythonPath),
                Version = string.Empty
            };
            if (pythonInfo.ExecutableExists)
            {
                pythonInfo.Version = await ReadPythonVersionAsync(pythonPath, workingDirectoryPath, ct);
            }
            else
            {
                notes.Add("Python executable not found.");
            }

            var workingDirectoryInfo = new DirectoryStatusInfo
            {
                Path = workingDirectoryPath,
                Exists = Directory.Exists(workingDirectoryPath)
            };
            if (!workingDirectoryInfo.Exists)
            {
                notes.Add("Working directory not found.");
            }

            var modelArtifacts = new ModelArtifactsInfo
            {
                ModelDirectoryPath = modelDirectoryPath,
                DirectoryExists = Directory.Exists(modelDirectoryPath),
                ModelFileExists = File.Exists(modelFilePath),
                MetricsFileExists = File.Exists(metricsFilePath),
                MetricsLastWriteUtc = File.Exists(metricsFilePath) ? File.GetLastWriteTimeUtc(metricsFilePath) : null,
                Metrics = LoadMetricsSummary(metricsFilePath, notes)
            };

            var predictCliAvailable = await _pythonApiService.HealthCheckAsync();
            if (!predictCliAvailable)
            {
                notes.Add("Predict CLI health check failed.");
            }

            var qualityGate = new QualityGateThresholds
            {
                MinPrecision = _options.MinPrecision,
                MinF1 = _options.MinF1,
                MinRocAuc = _options.MinRocAuc,
                MinPositives = _options.MinPositives
            };

            var status = ComputeStatus(
                pythonInfo.ExecutableExists,
                workingDirectoryInfo.Exists,
                modelArtifacts.ModelFileExists,
                modelArtifacts.MetricsFileExists,
                predictCliAvailable);

            return new IAStatusResponse
            {
                Status = status,
                CheckedAtUtc = checkedAtUtc,
                PredictCliAvailable = predictCliAvailable,
                Python = pythonInfo,
                WorkingDirectory = workingDirectoryInfo,
                ModelArtifacts = modelArtifacts,
                QualityGate = qualityGate,
                Notes = notes
            };
        }

        private static IAHealthStatusEnum ComputeStatus(
            bool pythonExists,
            bool workingDirectoryExists,
            bool modelFileExists,
            bool metricsFileExists,
            bool predictCliAvailable)
        {
            if (!pythonExists || !workingDirectoryExists || !predictCliAvailable)
            {
                return IAHealthStatusEnum.Down;
            }

            if (!modelFileExists || !metricsFileExists)
            {
                return IAHealthStatusEnum.Degraded;
            }

            return IAHealthStatusEnum.Up;
        }

        private static string BuildHealthMessage(IAStatusResponse status)
        {
            return status.Status switch
            {
                IAHealthStatusEnum.Up => "IA is operational.",
                IAHealthStatusEnum.Degraded => "IA is reachable but model artifacts are incomplete.",
                _ => "IA is not operational."
            };
        }

        private string ResolvePath(string configuredPath)
        {
            if (Path.IsPathRooted(configuredPath))
            {
                return configuredPath;
            }

            return Path.GetFullPath(Path.Combine(_environment.ContentRootPath, configuredPath));
        }

        private string ResolveModelDirectoryPath(string resolvedWorkingDirectory)
        {
            if (Path.IsPathRooted(_options.ModelDir))
            {
                return _options.ModelDir;
            }

            return Path.GetFullPath(Path.Combine(resolvedWorkingDirectory, _options.ModelDir));
        }

        private static ModelMetricsSummary LoadMetricsSummary(string metricsFilePath, List<string> notes)
        {
            if (!File.Exists(metricsFilePath))
            {
                return new ModelMetricsSummary();
            }

            try
            {
                using var document = JsonDocument.Parse(File.ReadAllText(metricsFilePath));
                var root = document.RootElement;
                var metricsNode = root.TryGetProperty("test", out var testNode) && testNode.ValueKind == JsonValueKind.Object
                    ? testNode
                    : root;

                return new ModelMetricsSummary
                {
                    Precision = ReadDecimalOrNull(metricsNode, "precision"),
                    F1 = ReadDecimalOrNull(metricsNode, "f1"),
                    RocAuc = ReadDecimalOrNull(metricsNode, "roc_auc"),
                    SelectedThreshold = ReadDecimalOrNull(root, "selected_threshold")
                };
            }
            catch (Exception)
            {
                notes.Add("metrics.json cannot be parsed.");
                return new ModelMetricsSummary();
            }
        }

        private static decimal? ReadDecimalOrNull(JsonElement node, string propertyName)
        {
            if (!node.TryGetProperty(propertyName, out var property) || property.ValueKind != JsonValueKind.Number)
            {
                return null;
            }

            return (decimal)property.GetDouble();
        }

        private static async Task<string> ReadPythonVersionAsync(
            string pythonExePath,
            string workingDirectoryPath,
            CancellationToken ct)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = pythonExePath,
                WorkingDirectory = workingDirectoryPath,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };
            startInfo.ArgumentList.Add("--version");

            using var process = new Process { StartInfo = startInfo };
            process.Start();
            var stdoutTask = process.StandardOutput.ReadToEndAsync(ct);
            var stderrTask = process.StandardError.ReadToEndAsync(ct);
            await process.WaitForExitAsync(ct);

            var stdout = await stdoutTask;
            var stderr = await stderrTask;
            var raw = string.IsNullOrWhiteSpace(stdout) ? stderr : stdout;
            return raw.Trim();
        }
    }
}
