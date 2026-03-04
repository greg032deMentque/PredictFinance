using BackPredictFinance.Common.enums;
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
        Task<SimulationOut> SimulateAsync(PythonSimulationRequest request);
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

            var request = new PythonPredictRequest
            {
                Symbol = symbol,
                ModelDir = _options.ModelDir,
                Period = _options.Period
            };

            var rawPayload = await RunPredictCliAsync(request);
            var parsed = ParsePredictPayload(rawPayload);
            var qualityGate = BuildModelQualityGate();
            var advice = BuildAction(parsed.LastProbability);
            if (qualityGate.ModelStatus == ModelStatusEnum.NoGo)
            {
                advice = (
                    "hold",
                    "Model quality gate is NO_GO, recommendation forced to hold.",
                    0.5m
                );
            }

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
                ModelStatus = qualityGate.ModelStatus,
                ModelChecks = qualityGate.ModelChecks,
                ModelMessage = qualityGate.ModelMessage,
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

        public async Task<SimulationOut> SimulateAsync(PythonSimulationRequest request)
        {
            if (request.InvestmentAmount <= 0m)
            {
                throw new ArgumentException("InvestmentAmount must be strictly positive", nameof(request.InvestmentAmount));
            }

            var normalizedSymbol = (request.Symbol ?? string.Empty).Trim().ToUpperInvariant();
            if (string.IsNullOrWhiteSpace(normalizedSymbol))
            {
                throw new ArgumentException("Symbol is required", nameof(request.Symbol));
            }

            var normalizedPattern = string.IsNullOrWhiteSpace(request.Pattern)
                ? "DOUBLE_TOP"
                : request.Pattern.Trim().ToUpperInvariant();
            if (normalizedPattern != "DOUBLE_TOP")
            {
                throw new ArgumentException("Only DOUBLE_TOP pattern is supported", nameof(request.Pattern));
            }

            var simulationRequest = new PythonSimulationRequest
            {
                Symbol = normalizedSymbol,
                Pattern = normalizedPattern,
                ModelDir = string.IsNullOrWhiteSpace(request.ModelDir) ? _options.ModelDir : request.ModelDir,
                Period = string.IsNullOrWhiteSpace(request.Period) ? _options.Period : request.Period,
                InvestmentAmount = request.InvestmentAmount,
                HorizonDays = Math.Clamp(request.HorizonDays, 1, 365),
                SellThreshold = request.SellThreshold <= 0m ? _options.SellThreshold : request.SellThreshold,
                BuyThreshold = request.BuyThreshold <= 0m ? _options.BuyThreshold : request.BuyThreshold
            };

            var rawPayload = await RunSimulateCliAsync(simulationRequest);
            var parsed = ParseSimulatePayload(rawPayload);

            return new SimulationOut
            {
                Symbol = parsed.Symbol,
                Pattern = parsed.Pattern,
                SimulatedAt = parsed.AsOf,
                InvestmentAmount = parsed.InvestmentAmount,
                HorizonDays = parsed.HorizonDays,
                EstimatedReturnPct = parsed.EstimatedReturnPct,
                EstimatedReturnAmount = parsed.EstimatedReturnAmount,
                EstimatedFinalAmount = parsed.EstimatedFinalAmount,
                Recommendation = parsed.Recommendation,
                Confidence = parsed.Confidence,
                Assumption = parsed.Assumption,
                LastProbability = parsed.LastProbability,
                MeanProbability = parsed.MeanProbability,
                MaxProbability = parsed.MaxProbability,
                NWindows = parsed.NWindows
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

        private async Task<string> RunPredictCliAsync(PythonPredictRequest request)
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
            startInfo.ArgumentList.Add(request.Symbol);
            startInfo.ArgumentList.Add("--model-dir");
            startInfo.ArgumentList.Add(request.ModelDir);
            startInfo.ArgumentList.Add("--period");
            startInfo.ArgumentList.Add(request.Period);

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
                _logger.LogError(ex, "Python CLI prediction failed for symbol {Symbol}", request.Symbol);
                throw;
            }
        }

        private async Task<string> RunSimulateCliAsync(PythonSimulationRequest request)
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
            startInfo.ArgumentList.Add("finance_ia.cli.simulate");
            startInfo.ArgumentList.Add("--ticker");
            startInfo.ArgumentList.Add(request.Symbol);
            startInfo.ArgumentList.Add("--model-dir");
            startInfo.ArgumentList.Add(request.ModelDir);
            startInfo.ArgumentList.Add("--period");
            startInfo.ArgumentList.Add(request.Period);
            startInfo.ArgumentList.Add("--pattern");
            startInfo.ArgumentList.Add(request.Pattern);
            startInfo.ArgumentList.Add("--investment-amount");
            startInfo.ArgumentList.Add(request.InvestmentAmount.ToString(CultureInfo.InvariantCulture));
            startInfo.ArgumentList.Add("--horizon-days");
            startInfo.ArgumentList.Add(request.HorizonDays.ToString(CultureInfo.InvariantCulture));
            startInfo.ArgumentList.Add("--sell-threshold");
            startInfo.ArgumentList.Add(request.SellThreshold.ToString(CultureInfo.InvariantCulture));
            startInfo.ArgumentList.Add("--buy-threshold");
            startInfo.ArgumentList.Add(request.BuyThreshold.ToString(CultureInfo.InvariantCulture));

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
                        $"Python simulate command failed (exit={process.ExitCode}): {stderr}");
                }

                if (string.IsNullOrWhiteSpace(stdout))
                {
                    throw new InvalidOperationException("Python simulate command returned empty output");
                }

                return stdout;
            }
            catch (OperationCanceledException)
            {
                if (!process.HasExited)
                {
                    process.Kill(entireProcessTree: true);
                }
                throw new TimeoutException($"Python simulate command timed out after {timeout.TotalSeconds:0}s");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Python CLI simulation failed for symbol {Symbol}", request.Symbol);
                throw;
            }
        }

        private PythonPredictPayload ParsePredictPayload(string json)
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

            return new PythonPredictPayload
            {
                Symbol = symbol,
                AsOf = asOf,
                MeanProbability = Clamp01(meanProb),
                MaxProbability = Clamp01(maxProb),
                LastProbability = Clamp01(lastProb),
                NWindows = Math.Max(0, nWindows)
            };
        }

        private PythonSimulationPayload ParseSimulatePayload(string json)
        {
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;

            var symbol = GetRequiredString(root, "ticker").ToUpperInvariant();
            var pattern = GetRequiredString(root, "pattern").ToUpperInvariant();
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

            return new PythonSimulationPayload
            {
                Symbol = symbol,
                Pattern = pattern,
                AsOf = DateTime.SpecifyKind(asOfDate, DateTimeKind.Utc),
                InvestmentAmount = GetRequiredDecimal(root, "investment_amount"),
                HorizonDays = GetRequiredInt(root, "horizon_days"),
                EstimatedReturnPct = GetRequiredDecimal(root, "estimated_return_pct"),
                EstimatedReturnAmount = GetRequiredDecimal(root, "estimated_return_amount"),
                EstimatedFinalAmount = GetRequiredDecimal(root, "estimated_final_amount"),
                Recommendation = GetRequiredString(root, "recommendation").ToLowerInvariant(),
                Confidence = GetRequiredDecimal(root, "confidence"),
                Assumption = GetRequiredString(root, "assumption"),
                LastProbability = GetRequiredDecimal(root, "last_prob"),
                MeanProbability = GetRequiredDecimal(root, "mean_prob"),
                MaxProbability = GetRequiredDecimal(root, "max_prob"),
                NWindows = GetRequiredInt(root, "n_windows")
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

        private ModelQualityGate BuildModelQualityGate()
        {
            var checks = new List<ModelCheckResult>();

            var metricsPath = ResolveModelMetricsPath();
            if (!File.Exists(metricsPath))
            {
                checks.Add(new ModelCheckResult
                {
                    Check = ModelCheckEnum.Precision,
                    Status = ModelCheckStatusEnum.NotApplicable,
                    Detail = $"metrics.json not found at {metricsPath}"
                });
                return new ModelQualityGate
                {
                    ModelStatus = ModelStatusEnum.NoGo,
                    ModelChecks = checks,
                    ModelMessage = "Model metrics are missing. Run training before using predictions."
                };
            }

            using var doc = JsonDocument.Parse(File.ReadAllText(metricsPath));
            var root = doc.RootElement;
            var testMetrics = root.TryGetProperty("test", out var testElement) && testElement.ValueKind == JsonValueKind.Object
                ? testElement
                : root;

            var precision = TryGetDecimal(testMetrics, "precision");
            var f1 = TryGetDecimal(testMetrics, "f1");
            var rocAuc = TryGetDecimal(testMetrics, "roc_auc");

            checks.Add(BuildNumericCheck(ModelCheckEnum.Precision, precision, _options.MinPrecision));
            checks.Add(BuildNumericCheck(ModelCheckEnum.F1, f1, _options.MinF1));
            checks.Add(BuildNumericCheck(ModelCheckEnum.RocAuc, rocAuc, _options.MinRocAuc));
            checks.Add(new ModelCheckResult
            {
                Check = ModelCheckEnum.MinimumPositives,
                Status = ModelCheckStatusEnum.NotApplicable,
                Threshold = _options.MinPositives,
                Detail = "Minimum positives count is not available in metrics.json."
            });

            var hasFail = checks.Any(x => x.Status == ModelCheckStatusEnum.Fail);
            return new ModelQualityGate
            {
                ModelStatus = hasFail ? ModelStatusEnum.NoGo : ModelStatusEnum.Go,
                ModelChecks = checks,
                ModelMessage = hasFail
                    ? "Model quality gate failed. Recommendation is forced to HOLD."
                    : "Model quality gate passed."
            };
        }

        private static ModelCheckResult BuildNumericCheck(ModelCheckEnum check, decimal? value, decimal threshold)
        {
            if (!value.HasValue)
            {
                return new ModelCheckResult
                {
                    Check = check,
                    Status = ModelCheckStatusEnum.NotApplicable,
                    Threshold = threshold,
                    Detail = "Metric not available."
                };
            }

            var passed = value.Value >= threshold;
            return new ModelCheckResult
            {
                Check = check,
                Status = passed ? ModelCheckStatusEnum.Pass : ModelCheckStatusEnum.Fail,
                Value = value,
                Threshold = threshold,
                Detail = passed ? "Threshold reached." : "Threshold not reached."
            };
        }

        private string ResolveModelMetricsPath()
        {
            var workingDirectory = ResolvePath(_options.WorkingDirectory);
            var modelDir = _options.ModelDir;
            var combined = Path.IsPathRooted(modelDir)
                ? modelDir
                : Path.Combine(workingDirectory, modelDir);
            return Path.Combine(combined, "metrics.json");
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

        private static decimal? TryGetDecimal(JsonElement root, string propertyName)
        {
            if (!root.TryGetProperty(propertyName, out var element))
            {
                return null;
            }

            return element.ValueKind switch
            {
                JsonValueKind.Number => (decimal)element.GetDouble(),
                _ => null
            };
        }

        private static decimal Clamp01(decimal value)
        {
            if (value < 0m) return 0m;
            if (value > 1m) return 1m;
            return value;
        }

    }
}
