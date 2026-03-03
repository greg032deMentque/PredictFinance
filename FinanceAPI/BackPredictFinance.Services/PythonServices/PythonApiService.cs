using BackPredictFinance.Services.PythonServices.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Diagnostics;
using System.Globalization;
using System.Text.Json;

namespace BackPredictFinance.Services.PythonServices
{
    public interface IPythonApiService
    {
        Task<PredictOut> PredictAsync(AssetIn asset);
        Task<RecommendationOut> RecommendAsync(RecommendationIn rec);
        Task<bool> HealthCheckAsync();
    }

    public class PythonApiService : IPythonApiService
    {
        private readonly PythonCliOptions _options;
        private readonly IHostEnvironment _environment;
        private readonly ILogger<PythonApiService> _logger;

        public PythonApiService(
            IOptions<PythonCliOptions> options,
            IHostEnvironment environment,
            ILogger<PythonApiService> logger)
        {
            _options = options.Value;
            _environment = environment;
            _logger = logger;
        }

        public async Task<PredictOut> PredictAsync(AssetIn asset)
        {
            var symbol = (asset.Symbol ?? string.Empty).Trim().ToUpperInvariant();
            if (string.IsNullOrWhiteSpace(symbol))
            {
                throw new ArgumentException("Symbol is required", nameof(asset.Symbol));
            }

            var rawPayload = await RunPredictCliAsync(symbol);
            var parsed = ParsePredictPayload(rawPayload);
            var advice = BuildAction(parsed.LastProbability);

            return new PredictOut
            {
                Symbol = parsed.Symbol,
                PredictedAt = parsed.AsOf,
                Pattern = "DOUBLE_TOP",
                LastProbability = parsed.LastProbability,
                MeanProbability = parsed.MeanProbability,
                MaxProbability = parsed.MaxProbability,
                ProbabilityPct = parsed.LastProbability * 100m,
                MeanProbabilityPct = parsed.MeanProbability * 100m,
                MaxProbabilityPct = parsed.MaxProbability * 100m,
                SuggestedAction = advice.Action,
                ActionReason = advice.Reason,
                ActionConfidence = advice.Confidence,
                NWindows = parsed.NWindows,
                Patterns =
                [
                    new PatternPrediction
                    {
                        Pattern = "DOUBLE_TOP",
                        Probability = parsed.LastProbability,
                    }
                ]
            };
        }

        public async Task<RecommendationOut> RecommendAsync(RecommendationIn rec)
        {
            var prediction = await PredictAsync(new AssetIn { Symbol = rec.Symbol });

            return new RecommendationOut
            {
                Symbol = prediction.Symbol,
                RecommendedAt = prediction.PredictedAt,
                Action = prediction.SuggestedAction,
                Confidence = prediction.ActionConfidence,
                Reason = prediction.ActionReason
            };
        }

        public async Task<bool> HealthCheckAsync()
        {
            try
            {
                var pythonExe = ResolvePath(_options.PythonExe);
                var workingDirectory = ResolvePath(_options.WorkingDirectory);
                if (!File.Exists(pythonExe) || !Directory.Exists(workingDirectory))
                {
                    return false;
                }

                var startInfo = new ProcessStartInfo
                {
                    FileName = pythonExe,
                    WorkingDirectory = workingDirectory,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                };
                startInfo.ArgumentList.Add("--version");

                using var process = new Process { StartInfo = startInfo };
                process.Start();
                await process.WaitForExitAsync();
                return process.ExitCode == 0;
            }
            catch
            {
                return false;
            }
        }

        private async Task<string> RunPredictCliAsync(string symbol)
        {
            var pythonExe = ResolvePath(_options.PythonExe);
            var workingDirectory = ResolvePath(_options.WorkingDirectory);
            if (!File.Exists(pythonExe))
            {
                throw new FileNotFoundException($"Python executable not found: {pythonExe}");
            }
            if (!Directory.Exists(workingDirectory))
            {
                throw new DirectoryNotFoundException($"Python working directory not found: {workingDirectory}");
            }

            var startInfo = new ProcessStartInfo
            {
                FileName = pythonExe,
                WorkingDirectory = workingDirectory,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };
            startInfo.ArgumentList.Add("-m");
            startInfo.ArgumentList.Add("finance_ia.cli.predict");
            startInfo.ArgumentList.Add("--ticker");
            startInfo.ArgumentList.Add(symbol);
            startInfo.ArgumentList.Add("--model-dir");
            startInfo.ArgumentList.Add(_options.ModelDir);
            startInfo.ArgumentList.Add("--period");
            startInfo.ArgumentList.Add(_options.Period);

            using var process = new Process { StartInfo = startInfo };
            var timeout = TimeSpan.FromSeconds(Math.Max(5, _options.TimeoutSeconds));
            using var timeoutCts = new CancellationTokenSource(timeout);

            try
            {
                process.Start();
                var stdoutTask = process.StandardOutput.ReadToEndAsync();
                var stderrTask = process.StandardError.ReadToEndAsync();

                await process.WaitForExitAsync(timeoutCts.Token);

                var stdout = await stdoutTask;
                var stderr = await stderrTask;

                if (process.ExitCode != 0)
                {
                    throw new InvalidOperationException(
                        $"Python predict command failed (exit={process.ExitCode}): {stderr}");
                }

                if (string.IsNullOrWhiteSpace(stdout))
                {
                    throw new InvalidOperationException("Python predict command returned empty output");
                }

                return stdout;
            }
            catch (OperationCanceledException)
            {
                if (!process.HasExited)
                {
                    process.Kill(entireProcessTree: true);
                }
                throw new TimeoutException($"Python predict command timed out after {timeout.TotalSeconds:0}s");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Python CLI prediction failed for symbol {Symbol}", symbol);
                throw;
            }
        }

        private ParsedPredictPayload ParsePredictPayload(string json)
        {
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;

            var symbol = GetRequiredString(root, "ticker").ToUpperInvariant();
            var asOfRaw = GetRequiredString(root, "as_of");
            if (!DateTime.TryParseExact(
                asOfRaw,
                "yyyy-MM-dd",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var asOfDate))
            {
                throw new FormatException($"Invalid 'as_of' date format: {asOfRaw}");
            }

            var asOf = DateTime.SpecifyKind(asOfDate, DateTimeKind.Utc);
            var meanProb = GetRequiredDecimal(root, "mean_prob");
            var maxProb = GetRequiredDecimal(root, "max_prob");
            var lastProb = GetRequiredDecimal(root, "last_prob");
            var nWindows = GetRequiredInt(root, "n_windows");

            return new ParsedPredictPayload
            {
                Symbol = symbol,
                AsOf = asOf,
                MeanProbability = Clamp01(meanProb),
                MaxProbability = Clamp01(maxProb),
                LastProbability = Clamp01(lastProb),
                NWindows = Math.Max(0, nWindows)
            };
        }

        private (string Action, string Reason, decimal Confidence) BuildAction(decimal lastProbability)
        {
            var sellThreshold = Clamp01(_options.SellThreshold);
            var buyThreshold = Clamp01(_options.BuyThreshold);
            if (buyThreshold > sellThreshold)
            {
                (buyThreshold, sellThreshold) = (sellThreshold, buyThreshold);
            }

            if (lastProbability >= sellThreshold)
            {
                return (
                    "sell",
                    $"Double Top probability {lastProbability:P1} is above sell threshold {sellThreshold:P1}",
                    lastProbability
                );
            }

            if (lastProbability <= buyThreshold)
            {
                return (
                    "buy",
                    $"Double Top probability {lastProbability:P1} is below buy threshold {buyThreshold:P1}",
                    1m - lastProbability
                );
            }

            var holdConfidence = Clamp01(1m - Math.Abs(lastProbability - 0.5m) * 2m);
            return (
                "hold",
                $"Double Top probability {lastProbability:P1} is between thresholds ({buyThreshold:P1} - {sellThreshold:P1})",
                holdConfidence
            );
        }

        private string ResolvePath(string pathValue)
        {
            if (Path.IsPathRooted(pathValue))
            {
                return pathValue;
            }

            return Path.GetFullPath(Path.Combine(_environment.ContentRootPath, pathValue));
        }

        private static string GetRequiredString(JsonElement root, string propertyName)
        {
            if (!root.TryGetProperty(propertyName, out var element) || element.ValueKind != JsonValueKind.String)
            {
                throw new FormatException($"Missing or invalid '{propertyName}'");
            }

            return element.GetString() ?? string.Empty;
        }

        private static decimal GetRequiredDecimal(JsonElement root, string propertyName)
        {
            if (!root.TryGetProperty(propertyName, out var element) || element.ValueKind != JsonValueKind.Number)
            {
                throw new FormatException($"Missing or invalid '{propertyName}'");
            }

            return (decimal)element.GetDouble();
        }

        private static int GetRequiredInt(JsonElement root, string propertyName)
        {
            if (!root.TryGetProperty(propertyName, out var element) || element.ValueKind != JsonValueKind.Number)
            {
                throw new FormatException($"Missing or invalid '{propertyName}'");
            }

            return element.GetInt32();
        }

        private static decimal Clamp01(decimal value)
        {
            if (value < 0m) return 0m;
            if (value > 1m) return 1m;
            return value;
        }

        private sealed class ParsedPredictPayload
        {
            public string Symbol { get; init; } = string.Empty;
            public DateTime AsOf { get; init; }
            public decimal MeanProbability { get; init; }
            public decimal MaxProbability { get; init; }
            public decimal LastProbability { get; init; }
            public int NWindows { get; init; }
        }
    }
}
