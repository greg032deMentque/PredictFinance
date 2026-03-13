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
        private readonly IPatternCatalogService _patternCatalogService;
        private readonly IHostEnvironment _environment;
        private readonly ILogger<PythonApiService> _logger;

        public PythonApiService(
            IOptions<PythonCliOptions> options,
            IPatternCatalogService patternCatalogService,
            IHostEnvironment environment,
            ILogger<PythonApiService> logger)
        {
            _options = options.Value;
            _patternCatalogService = patternCatalogService;
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

            var normalizedPattern = string.IsNullOrWhiteSpace(asset.Pattern)
                ? "DOUBLE_TOP"
                : asset.Pattern.Trim().ToUpperInvariant();
            if (normalizedPattern != "DOUBLE_TOP")
            {
                throw new ArgumentException("Only DOUBLE_TOP pattern is supported", nameof(asset.Pattern));
            }
            var patternConfiguration = _patternCatalogService.Resolve(normalizedPattern);

            var request = new PythonPredictRequest
            {
                Symbol = symbol,
                Pattern = normalizedPattern,
                ModelDir = patternConfiguration.ModelDir,
                Period = _options.Period
            };

            var rawPayload = await RunPredictCliAsync(request);
            var parsed = ParsePredictPayload(rawPayload);
            var qualityGate = BuildModelQualityGate(patternConfiguration.ModelDir);
            var primaryAssessment = GetPrimaryAssessment(parsed.PatternAssessments);

            return new PredictOut
            {
                Symbol = parsed.Symbol,
                PredictedAt = parsed.AsOf,
                Pattern = parsed.Pattern,
                Phase = parsed.Phase,
                CurrentPrice = parsed.CurrentPrice,
                LastProbability = parsed.LastProbability,
                MeanProbability = parsed.MeanProbability,
                MaxProbability = parsed.MaxProbability,
                ProbabilityPct = parsed.LastProbability * 100m,
                MeanProbabilityPct = parsed.MeanProbability * 100m,
                MaxProbabilityPct = parsed.MaxProbability * 100m,
                SuggestedAction = parsed.DecisionSignal.Action,
                IsActionable = parsed.DecisionSignal.Actionable,
                ActionReason = parsed.DecisionSignal.Reason,
                ActionConfidence = parsed.DecisionSignal.Confidence,
                HorizonDays = parsed.DecisionSignal.HorizonDays,
                TargetPrice = primaryAssessment?.TargetPrice,
                InvalidationPrice = primaryAssessment?.InvalidationPrice,
                NecklinePrice = primaryAssessment?.NecklinePrice,
                NWindows = parsed.NWindows,
                ModelStatus = qualityGate.ModelStatus,
                ModelChecks = qualityGate.ModelChecks,
                ModelMessage = qualityGate.ModelMessage,
                ModelVersion = patternConfiguration.ModelVersion,
                Precision = qualityGate.Metrics.Precision,
                F1 = qualityGate.Metrics.F1,
                RocAuc = qualityGate.Metrics.RocAuc,
                PositiveSamples = qualityGate.Metrics.PositiveSamples,
                SelectedThreshold = qualityGate.Metrics.SelectedThreshold,
                Patterns = parsed.PatternAssessments
                    .Select(assessment => new PatternPrediction
                    {
                        Pattern = assessment.Pattern,
                        Phase = assessment.Phase,
                        Probability = assessment.Probability,
                        Confidence = assessment.Confidence,
                        CurrentPrice = assessment.CurrentPrice,
                        NecklinePrice = assessment.NecklinePrice,
                        TargetPrice = assessment.TargetPrice,
                        InvalidationPrice = assessment.InvalidationPrice,
                        FirstPeakAtUtc = assessment.FirstPeakAtUtc,
                        SecondPeakAtUtc = assessment.SecondPeakAtUtc,
                        IsPrimary = assessment.IsPrimary
                    })
                    .ToList()
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
            var patternConfiguration = _patternCatalogService.Resolve(normalizedPattern);

            var simulationRequest = new PythonSimulationRequest
            {
                Symbol = normalizedSymbol,
                Pattern = normalizedPattern,
                ModelDir = string.IsNullOrWhiteSpace(request.ModelDir) ? patternConfiguration.ModelDir : request.ModelDir,
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
            startInfo.ArgumentList.Add("--pattern");
            startInfo.ArgumentList.Add(request.Pattern);

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
            var pattern = GetRequiredString(root, "pattern").Trim().ToUpperInvariant();
            var phase = GetRequiredString(root, "phase");
            var currentPrice = GetRequiredDecimal(root, "current_price");
            var meanProb = GetRequiredDecimal(root, "mean_prob");
            var maxProb = GetRequiredDecimal(root, "max_prob");
            var lastProb = GetRequiredDecimal(root, "last_prob");
            var nWindows = GetRequiredInt(root, "n_windows");
            var patternAssessments = ParsePatternAssessments(root);
            var decisionSignal = ParseDecisionSignal(root);

            return new PythonPredictPayload
            {
                Symbol = symbol,
                Pattern = pattern,
                Phase = phase,
                AsOf = asOf,
                CurrentPrice = currentPrice,
                MeanProbability = Clamp01(meanProb),
                MaxProbability = Clamp01(maxProb),
                LastProbability = Clamp01(lastProb),
                NWindows = Math.Max(0, nWindows),
                PatternAssessments = patternAssessments,
                DecisionSignal = decisionSignal
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

        private ModelQualityGate BuildModelQualityGate(string modelDir)
        {
            var checks = new List<ModelCheckResult>();

            var metricsPath = ResolveModelMetricsPath(modelDir);
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
                Metrics = new ModelMetricsSummary
                {
                    Precision = precision,
                    F1 = f1,
                    RocAuc = rocAuc,
                    PositiveSamples = TryGetInt(testMetrics, "positive_samples"),
                    SelectedThreshold = TryGetDecimal(root, "selected_threshold")
                },
                ModelMessage = hasFail
                    ? "Model quality gate failed. Use prediction output with caution."
                    : "Model quality gate passed."
            };
        }

        private static PythonPatternAssessmentPayload? GetPrimaryAssessment(List<PythonPatternAssessmentPayload> assessments)
        {
            return assessments
                .OrderByDescending(x => x.IsPrimary)
                .ThenByDescending(x => x.Confidence)
                .ThenByDescending(x => x.Probability)
                .FirstOrDefault();
        }

        private static List<PythonPatternAssessmentPayload> ParsePatternAssessments(JsonElement root)
        {
            if (!root.TryGetProperty("pattern_assessments", out var assessmentsElement) ||
                assessmentsElement.ValueKind != JsonValueKind.Array)
            {
                return [];
            }

            var assessments = new List<PythonPatternAssessmentPayload>();
            foreach (var assessmentElement in assessmentsElement.EnumerateArray())
            {
                if (assessmentElement.ValueKind != JsonValueKind.Object)
                {
                    continue;
                }

                assessments.Add(new PythonPatternAssessmentPayload
                {
                    Pattern = GetRequiredString(assessmentElement, "pattern").Trim().ToUpperInvariant(),
                    Phase = GetRequiredString(assessmentElement, "phase"),
                    Probability = Clamp01(GetRequiredDecimal(assessmentElement, "probability")),
                    Confidence = Clamp01(GetRequiredDecimal(assessmentElement, "confidence")),
                    CurrentPrice = GetRequiredDecimal(assessmentElement, "current_price"),
                    NecklinePrice = GetOptionalDecimal(assessmentElement, "neckline_price"),
                    TargetPrice = GetOptionalDecimal(assessmentElement, "target_price"),
                    InvalidationPrice = GetOptionalDecimal(assessmentElement, "invalidation_price"),
                    FirstPeakAtUtc = GetOptionalDateTime(assessmentElement, "first_peak_at"),
                    SecondPeakAtUtc = GetOptionalDateTime(assessmentElement, "second_peak_at"),
                    IsPrimary = GetOptionalBool(assessmentElement, "is_primary") ?? false
                });
            }

            return assessments;
        }

        private static PythonDecisionSignalPayload ParseDecisionSignal(JsonElement root)
        {
            if (!root.TryGetProperty("decision_signal", out var decisionElement) ||
                decisionElement.ValueKind != JsonValueKind.Object)
            {
                throw new FormatException("Missing or invalid 'decision_signal'");
            }

            return new PythonDecisionSignalPayload
            {
                Action = GetRequiredString(decisionElement, "action").Trim().ToLowerInvariant(),
                Actionable = GetRequiredBool(decisionElement, "actionable"),
                Confidence = Clamp01(GetRequiredDecimal(decisionElement, "confidence")),
                Reason = GetRequiredString(decisionElement, "reason"),
                HorizonDays = Math.Max(0, GetRequiredInt(decisionElement, "horizon_days"))
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

        private string ResolveModelMetricsPath(string modelDir)
        {
            var workingDirectory = ResolvePath(_options.WorkingDirectory);
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

        private static bool GetRequiredBool(JsonElement root, string propertyName)
        {
            if (!root.TryGetProperty(propertyName, out var element) ||
                (element.ValueKind != JsonValueKind.True && element.ValueKind != JsonValueKind.False))
            {
                throw new FormatException($"Missing or invalid '{propertyName}'");
            }

            return element.GetBoolean();
        }

        private static decimal? GetOptionalDecimal(JsonElement root, string propertyName)
        {
            if (!root.TryGetProperty(propertyName, out var element) || element.ValueKind == JsonValueKind.Null)
            {
                return null;
            }

            if (element.ValueKind != JsonValueKind.Number)
            {
                throw new FormatException($"Invalid '{propertyName}'");
            }

            return (decimal)element.GetDouble();
        }

        private static bool? GetOptionalBool(JsonElement root, string propertyName)
        {
            if (!root.TryGetProperty(propertyName, out var element) || element.ValueKind == JsonValueKind.Null)
            {
                return null;
            }

            if (element.ValueKind != JsonValueKind.True && element.ValueKind != JsonValueKind.False)
            {
                throw new FormatException($"Invalid '{propertyName}'");
            }

            return element.GetBoolean();
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

        private static int? TryGetInt(JsonElement root, string propertyName)
        {
            if (!root.TryGetProperty(propertyName, out var element))
            {
                return null;
            }

            return element.ValueKind switch
            {
                JsonValueKind.Number => element.GetInt32(),
                _ => null
            };
        }

        private static DateTime? GetOptionalDateTime(JsonElement root, string propertyName)
        {
            if (!root.TryGetProperty(propertyName, out var element) || element.ValueKind == JsonValueKind.Null)
            {
                return null;
            }

            if (element.ValueKind != JsonValueKind.String)
            {
                throw new FormatException($"Invalid '{propertyName}'");
            }

            var rawValue = element.GetString();
            if (string.IsNullOrWhiteSpace(rawValue))
            {
                return null;
            }

            if (!DateTime.TryParse(
                rawValue,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                out var parsed))
            {
                throw new FormatException($"Invalid '{propertyName}' date format: {rawValue}");
            }

            return DateTime.SpecifyKind(parsed, DateTimeKind.Utc);
        }

        private static decimal Clamp01(decimal value)
        {
            if (value < 0m) return 0m;
            if (value > 1m) return 1m;
            return value;
        }

    }
}
