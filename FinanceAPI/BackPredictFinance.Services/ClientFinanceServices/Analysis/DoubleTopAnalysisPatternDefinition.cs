using BackPredictFinance.Contracts.MarketData;
using BackPredictFinance.Contracts.Analysis;
using BackPredictFinance.Common.enums;
using BackPredictFinance.Services.TwelveDataServices;
using BackPredictFinance.Common.AnalysisV1;
using BackPredictFinance.ViewModels.ClientFinanceViewModels.AnalysisV1;
using System.Globalization;
using System.Text.Json;

namespace BackPredictFinance.Services.ClientFinanceServices.Analysis
{

public interface IAnalysisPatternDefinition
{
    string PatternId { get; }
    string ModelVersion { get; }
    int HistoryLookbackMonths { get; }
    ResolvedAnalysisPattern BuildResolvedPattern();
    Task<AnalysisExecutionArtifact> ExecuteAsync(AnalysisRequest request, CancellationToken ct = default);
}


    public sealed class DoubleTopAnalysisPatternDefinition : IAnalysisPatternDefinition
    {
        private const string SupportedPatternId = "DOUBLE_TOP";
        private const string AnalysisEngineVersion = "analysis-v1-deterministic-double-top@prompt7";
        private const int MinimumRequiredCandles = 47;
        private static readonly DoubleTopConfig Config = new();
        private readonly ITickerService _tickerService;

        public DoubleTopAnalysisPatternDefinition(ITickerService tickerService)
        {
            _tickerService = tickerService;
        }

        public string PatternId => SupportedPatternId;
        public string ModelVersion => AnalysisEngineVersion;
        public int HistoryLookbackMonths => 6;

        public ResolvedAnalysisPattern BuildResolvedPattern()
        {
            return new ResolvedAnalysisPattern
            {
                PatternId = SupportedPatternId,
                ModelVersion = AnalysisEngineVersion,
                HistoryLookbackMonths = HistoryLookbackMonths
            };
        }

        public async Task<AnalysisExecutionArtifact> ExecuteAsync(AnalysisRequest request, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(request);

            var resolvedPatterns = request.ResolvedPatternIds
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim().ToUpperInvariant())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (resolvedPatterns.Count != 1 || !string.Equals(resolvedPatterns[0], SupportedPatternId, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Le moteur d'analyse V1 actif prend en charge uniquement le pattern DOUBLE_TOP.");
            }

            var requestedCandleCount = BuildRequestedCandleCount(request);
            var timeSeries = await _tickerService.GetTimeSeriesAsync(
                request.Instrument.Symbol,
                string.IsNullOrWhiteSpace(request.CandleInterval) ? "1d" : request.CandleInterval.Trim(),
                requestedCandleCount,
                ct);

            var candles = timeSeries.Candles
                .OrderBy(x => x.Date)
                .ToList();

            if (candles.Count == 0)
            {
                throw new InvalidOperationException("Aucune donnee de marche exploitable n'a ete retournee pour l'analyse.");
            }

            var patternArtifact = BuildDoubleTopPatternArtifact(request, candles);
            var rawPayload = JsonSerializer.Serialize(BuildRawPayload(request, candles, patternArtifact));

            return new AnalysisExecutionArtifact
            {
                Symbol = request.Instrument.Symbol,
                GeneratedAtUtc = candles[^1].Date,
                Patterns = [patternArtifact],
                ModelStatus = ModelStatusEnum.Go,
                ModelMessage = "Analyse V1 produite par le moteur deterministe API.",
                ModelVersion = AnalysisEngineVersion,
                RawProviderPayloadJson = rawPayload
            };
        }

        private static int BuildRequestedCandleCount(AnalysisRequest request)
        {
            var calendarSpan = request.HistoryEndDate.DayNumber - request.HistoryStartDate.DayNumber + 1;
            return Math.Clamp(Math.Max(calendarSpan, MinimumRequiredCandles), MinimumRequiredCandles, 500);
        }

        private static ExecutedPatternArtifact BuildDoubleTopPatternArtifact(AnalysisRequest request, List<TickerCandle> candles)
        {
            var state = AnalyzeDoubleTopPhase(candles);
            var confidence = BuildConfidence(state);
            var isCompatible = IsCompatiblePhase(state.Phase);
            var status = MapStatus(state.Phase);
            var assessmentId = Guid.NewGuid().ToString("N");

            var assessment = new PatternAssessment
            {
                AssessmentId = assessmentId,
                PatternId = SupportedPatternId,
                DisplayName = "Double sommet",
                PedagogicalDescription = "Schema de retournement baissier avec deux sommets proches.",
                AnalysisWindow = new PatternAnalysisWindow
                {
                    Interval = string.IsNullOrWhiteSpace(request.CandleInterval) ? "1d" : request.CandleInterval.Trim(),
                    StartDate = request.HistoryStartDate,
                    EndDate = request.HistoryEndDate,
                    RequiredCandles = MinimumRequiredCandles,
                    ActualCandles = candles.Count
                },
                Detection = new PatternDetection
                {
                    IsCompatible = isCompatible,
                    Status = status,
                    CurrentPhaseCode = state.Phase,
                    CurrentPhaseLabel = BuildPhaseLabel(state.Phase),
                    StatusReason = BuildStatusReason(state),
                    CurrentPrice = state.CurrentPrice,
                    StructuralPoints = BuildStructuralPoints(state)
                },
                Validation = new PatternValidation
                {
                    State = IsValidatedPhase(state.Phase) ? "VALIDATED" : "NOT_VALIDATED",
                    Reason = BuildValidationReason(state.Phase),
                    ValidatedAtDate = IsValidatedPhase(state.Phase) ? DateOnly.FromDateTime(candles[^1].Date) : null,
                    ValidatedAtPrice = IsValidatedPhase(state.Phase) ? state.CurrentPrice : null,
                    ValidationRuleCode = IsValidatedPhase(state.Phase) ? "DOUBLE_TOP_NECKLINE_BREAK" : null
                },
                Invalidation = new PatternInvalidation
                {
                    State = state.Phase == "invalidated" ? "INVALIDATED" : "ACTIVE",
                    Reason = BuildInvalidationReason(state.Phase),
                    InvalidationLevel = state.InvalidationPrice,
                    BreachedAtDate = state.Phase == "invalidated" ? DateOnly.FromDateTime(candles[^1].Date) : null,
                    BreachedAtPrice = state.Phase == "invalidated" ? state.CurrentPrice : null,
                    InvalidationRuleCode = state.Phase == "invalidated" ? "DOUBLE_TOP_INVAL_HIGHER_TOP" : null
                },
                Scoring = new PatternScoring
                {
                    ConfidenceScore = confidence,
                    ConfidenceLabel = BuildConfidenceLabel(confidence),
                    IsCredible = isCompatible,
                    ScoreReasons = BuildScoreReasons(state, confidence),
                    ScoreVersion = AnalysisEngineVersion
                },
                RiskHints = new PatternRiskHints(),
                Explanation = new PatternExplanation(),
                Trace = new PatternTrace
                {
                    PatternVersion = AnalysisEngineVersion,
                    RuleSetVersion = AnalysisEngineVersion,
                    IsPrimaryDisplayCandidate = true,
                    ScoringVersion = AnalysisEngineVersion
                }
            };

            return new ExecutedPatternArtifact
            {
                Pattern = TradingPatternEnum.DoubleTop,
                Phase = state.Phase,
                Probability = confidence,
                Confidence = confidence,
                CurrentPrice = state.CurrentPrice,
                NecklinePrice = state.NecklinePrice,
                TargetPrice = state.TargetPrice,
                InvalidationPrice = state.InvalidationPrice,
                FirstPeakAtUtc = state.FirstPeakAtUtc,
                SecondPeakAtUtc = state.SecondPeakAtUtc,
                IsPrimary = true,
                ContractAssessment = assessment
            };
        }

        private static DeterministicDoubleTopState AnalyzeDoubleTopPhase(List<TickerCandle> candles)
        {
            var currentPrice = candles[^1].Close;
            if (candles.Count < MinimumRequiredCandles)
            {
                return new DeterministicDoubleTopState
                {
                    Phase = "candidate_first_peak",
                    CurrentPrice = currentPrice
                };
            }

            var peaks = FindLocalPeakIndices(candles, Config.PeakWindow);
            var pairs = DetectDoubleTopPairs(candles, peaks);

            if (pairs.Count > 0)
            {
                var latestPair = pairs
                    .OrderByDescending(x => x.SecondPeakIndex)
                    .First();

                var phase = currentPrice > latestPair.InvalidationPrice
                    ? "invalidated"
                    : currentPrice < latestPair.NecklinePrice
                        ? latestPair.SecondPeakIndex < candles.Count - 1 && ComputeRelativeGap(currentPrice, latestPair.NecklinePrice) <= 0.02m
                            ? "pullback_after_break"
                            : "neckline_break_confirmed"
                        : "second_peak_candidate";

                return new DeterministicDoubleTopState
                {
                    Phase = phase,
                    CurrentPrice = currentPrice,
                    NecklinePrice = latestPair.NecklinePrice,
                    InvalidationPrice = latestPair.InvalidationPrice,
                    TargetPrice = latestPair.TargetPrice,
                    FirstPeakAtUtc = candles[latestPair.FirstPeakIndex].Date,
                    SecondPeakAtUtc = candles[latestPair.SecondPeakIndex].Date,
                    FirstPeakPrice = latestPair.FirstPeakPrice,
                    ValleyPrice = latestPair.ValleyPrice,
                    SecondPeakPrice = latestPair.SecondPeakPrice,
                    Pair = latestPair,
                    PretrendMove = latestPair.PretrendMove,
                    PeakDiffRatio = latestPair.PeakDiffRatio,
                    ValleyDropRatio = latestPair.ValleyDropRatio
                };
            }

            if (peaks.Count > 0)
            {
                var latestPeakIndex = peaks[^1];
                var latestPeakPrice = candles[latestPeakIndex].Close;
                var valleySincePeak = candles.Skip(latestPeakIndex).Min(x => x.Close);
                var valleyDropRatio = latestPeakPrice <= 0m ? 0m : (latestPeakPrice - valleySincePeak) / latestPeakPrice;
                var phase = valleyDropRatio >= Config.ValleyDropPct ? "valley_confirmed" : "candidate_first_peak";

                return new DeterministicDoubleTopState
                {
                    Phase = phase,
                    CurrentPrice = currentPrice,
                    InvalidationPrice = latestPeakPrice * (1m + Config.PeakTolerancePct),
                    FirstPeakAtUtc = candles[latestPeakIndex].Date,
                    FirstPeakPrice = latestPeakPrice,
                    ValleyPrice = valleySincePeak,
                    ValleyDropRatio = valleyDropRatio
                };
            }

            return new DeterministicDoubleTopState
            {
                Phase = "candidate_first_peak",
                CurrentPrice = currentPrice
            };
        }

        private static List<int> FindLocalPeakIndices(List<TickerCandle> candles, int window)
        {
            var peaks = new List<int>();
            if (candles.Count < (2 * window + 1))
            {
                return peaks;
            }

            for (var index = window; index < candles.Count - window; index++)
            {
                var center = candles[index].Close;
                var isPeak = true;
                var equalCount = 0;

                for (var cursor = index - window; cursor <= index + window; cursor++)
                {
                    var candidate = candles[cursor].Close;
                    if (candidate > center)
                    {
                        isPeak = false;
                        break;
                    }

                    if (candidate == center)
                    {
                        equalCount++;
                    }
                }

                if (isPeak && equalCount == 1)
                {
                    peaks.Add(index);
                }
            }

            return peaks;
        }

        private static List<DoubleTopPair> DetectDoubleTopPairs(List<TickerCandle> candles, List<int> peaks)
        {
            var detected = new List<DoubleTopPair>();
            for (var leftIndex = 0; leftIndex < peaks.Count; leftIndex++)
            {
                var firstPeak = peaks[leftIndex];
                for (var rightIndex = leftIndex + 1; rightIndex < peaks.Count; rightIndex++)
                {
                    var secondPeak = peaks[rightIndex];
                    if ((secondPeak - firstPeak) > Config.MaxPeakDistance)
                    {
                        break;
                    }

                    var pair = BuildPair(candles, firstPeak, secondPeak);
                    if (pair != null)
                    {
                        detected.Add(pair);
                    }
                }
            }

            return detected;
        }

        private static DoubleTopPair? BuildPair(List<TickerCandle> candles, int firstPeakIndex, int secondPeakIndex)
        {
            var distance = secondPeakIndex - firstPeakIndex;
            if (distance < Config.MinPeakDistance || distance > Config.MaxPeakDistance)
            {
                return null;
            }

            var firstPeakPrice = candles[firstPeakIndex].Close;
            var secondPeakPrice = candles[secondPeakIndex].Close;
            var topReference = Math.Max(firstPeakPrice, secondPeakPrice);
            if (topReference <= 0m)
            {
                return null;
            }

            var peakDiffRatio = Math.Abs(firstPeakPrice - secondPeakPrice) / topReference;
            if (peakDiffRatio > Config.PeakTolerancePct)
            {
                return null;
            }

            var valleySlice = candles
                .Skip(firstPeakIndex)
                .Take(distance + 1)
                .ToList();

            var valleyPrice = valleySlice.Min(x => x.Close);
            var valleyIndex = firstPeakIndex + valleySlice.FindIndex(x => x.Close == valleyPrice);
            var valleyDropRatio = (topReference - valleyPrice) / topReference;
            if (valleyDropRatio < Config.ValleyDropPct)
            {
                return null;
            }

            var pretrendMove = ComputePretrendMove(candles, firstPeakIndex);
            if (pretrendMove < Config.MinPretrendPct)
            {
                return null;
            }

            var necklinePrice = valleyPrice;
            var invalidationPrice = topReference * (1m + Config.PeakTolerancePct);
            var targetPrice = necklinePrice - (topReference - necklinePrice);

            return new DoubleTopPair
            {
                FirstPeakIndex = firstPeakIndex,
                ValleyIndex = valleyIndex,
                SecondPeakIndex = secondPeakIndex,
                FirstPeakPrice = firstPeakPrice,
                ValleyPrice = valleyPrice,
                SecondPeakPrice = secondPeakPrice,
                NecklinePrice = necklinePrice,
                InvalidationPrice = invalidationPrice,
                TargetPrice = targetPrice,
                PretrendMove = pretrendMove,
                PeakDiffRatio = peakDiffRatio,
                ValleyDropRatio = valleyDropRatio,
                ValleyAtUtc = candles[valleyIndex].Date
            };
        }

        private static decimal ComputePretrendMove(List<TickerCandle> candles, int peakIndex)
        {
            var startIndex = peakIndex - Config.PretrendLookback;
            if (startIndex < 0)
            {
                return 0m;
            }

            var startPrice = candles[startIndex].Close;
            var peakPrice = candles[peakIndex].Close;
            if (startPrice <= 0m)
            {
                return 0m;
            }

            return (peakPrice / startPrice) - 1m;
        }

        private static decimal BuildConfidence(DeterministicDoubleTopState state)
        {
            var baseScore = state.Phase switch
            {
                "pullback_after_break" => 0.78m,
                "neckline_break_confirmed" => 0.74m,
                "second_peak_candidate" => 0.58m,
                "valley_confirmed" => 0.34m,
                "invalidated" => 0.18m,
                _ => 0.12m
            };

            if (state.Pair == null)
            {
                return Math.Round(Math.Clamp(baseScore, 0m, 0.95m), 4);
            }

            var peakSimilarityBonus = (1m - Math.Clamp(state.PeakDiffRatio / Config.PeakTolerancePct, 0m, 1m)) * 0.10m;
            var valleyDepthBonus = Math.Clamp(state.ValleyDropRatio / Config.ValleyDropPct, 0m, 1m) * 0.04m;
            var pretrendBonus = Math.Clamp(state.PretrendMove / Config.MinPretrendPct, 0m, 1m) * 0.03m;

            return Math.Round(Math.Clamp(baseScore + peakSimilarityBonus + valleyDepthBonus + pretrendBonus, 0m, 0.95m), 4);
        }

        private static bool IsCompatiblePhase(string phase)
        {
            return phase switch
            {
                "second_peak_candidate" => true,
                "neckline_break_confirmed" => true,
                "pullback_after_break" => true,
                _ => false
            };
        }

        private static bool IsValidatedPhase(string phase)
        {
            return phase is "neckline_break_confirmed" or "pullback_after_break";
        }

        private static PatternStatus MapStatus(string phase)
        {
            return phase switch
            {
                "neckline_break_confirmed" => PatternStatus.Confirmed,
                "pullback_after_break" => PatternStatus.Confirmed,
                "invalidated" => PatternStatus.Invalidated,
                "second_peak_candidate" => PatternStatus.Monitoring,
                "valley_confirmed" => PatternStatus.Monitoring,
                _ => PatternStatus.Forming
            };
        }

        private static string BuildPhaseLabel(string phase)
        {
            return phase switch
            {
                "candidate_first_peak" => "Premier sommet candidat",
                "valley_confirmed" => "Creux intermediaire confirme",
                "second_peak_candidate" => "Second sommet candidat",
                "neckline_break_confirmed" => "Cassure de neckline confirmee",
                "pullback_after_break" => "Pullback apres cassure",
                "invalidated" => "Invalide",
                _ => "En formation"
            };
        }

        private static string BuildStatusReason(DeterministicDoubleTopState state)
        {
            return state.Phase switch
            {
                "candidate_first_peak" => $"Le marche ne montre pour l'instant qu'un premier sommet possible autour de {state.CurrentPrice.ToString(CultureInfo.InvariantCulture)}.",
                "valley_confirmed" => $"Le repli intermediaire est visible, mais le second sommet n'est pas encore confirme autour de {state.CurrentPrice.ToString(CultureInfo.InvariantCulture)}.",
                "second_peak_candidate" => $"Deux sommets proches restent compatibles et la cassure de neckline n'est pas encore confirmee au prix {state.CurrentPrice.ToString(CultureInfo.InvariantCulture)}.",
                "neckline_break_confirmed" => $"La cassure de neckline confirme le scenario baissier au prix {state.CurrentPrice.ToString(CultureInfo.InvariantCulture)}.",
                "pullback_after_break" => $"Le marche reste sous la neckline avec un pullback controle autour de {state.CurrentPrice.ToString(CultureInfo.InvariantCulture)}.",
                "invalidated" => $"Le prix courant {state.CurrentPrice.ToString(CultureInfo.InvariantCulture)} repasse au-dessus du niveau d'invalidation du scenario.",
                _ => $"Le scenario reste en observation autour de {state.CurrentPrice.ToString(CultureInfo.InvariantCulture)}."
            };
        }

        private static string BuildValidationReason(string phase)
        {
            return phase switch
            {
                "neckline_break_confirmed" => "La cassure de neckline confirme la structure de double sommet.",
                "pullback_after_break" => "Le pullback reste sous la neckline et confirme la structure de double sommet.",
                _ => "La structure n'a pas encore atteint un etat confirme."
            };
        }

        private static string BuildInvalidationReason(string phase)
        {
            return phase == "invalidated"
                ? "Le prix a depasse le niveau d'invalidation du double sommet."
                : "Le niveau d'invalidation reste actif tant que le scenario n'est pas rompu.";
        }

        private static string BuildConfidenceLabel(decimal confidence)
        {
            if (confidence >= 0.75m)
            {
                return "HIGH";
            }

            if (confidence >= 0.45m)
            {
                return "MEDIUM";
            }

            return "LOW";
        }

        private static List<string> BuildScoreReasons(DeterministicDoubleTopState state, decimal confidence)
        {
            var reasons = new List<string>
            {
                $"Confiance deterministe calculee a {Math.Round(confidence * 100m, 2).ToString(CultureInfo.InvariantCulture)}%."
            };

            if (state.Pair != null)
            {
                reasons.Add($"Ecart relatif entre sommets: {Math.Round(state.PeakDiffRatio * 100m, 2).ToString(CultureInfo.InvariantCulture)}%.");
                reasons.Add($"Amplitude du creux intermediaire: {Math.Round(state.ValleyDropRatio * 100m, 2).ToString(CultureInfo.InvariantCulture)}%.");
                reasons.Add($"Hausse preparatoire observee avant le premier sommet: {Math.Round(state.PretrendMove * 100m, 2).ToString(CultureInfo.InvariantCulture)}%.");
            }

            return reasons;
        }

        private static List<PatternStructuralPoint> BuildStructuralPoints(DeterministicDoubleTopState state)
        {
            var points = new List<PatternStructuralPoint>();

            if (state.FirstPeakAtUtc.HasValue && state.FirstPeakPrice.HasValue)
            {
                points.Add(new PatternStructuralPoint
                {
                    PointType = "FIRST_PEAK",
                    Timestamp = state.FirstPeakAtUtc.Value,
                    Price = state.FirstPeakPrice.Value
                });
            }

            if (state.Pair != null && state.ValleyPrice.HasValue)
            {
                points.Add(new PatternStructuralPoint
                {
                    PointType = "NECKLINE",
                    Timestamp = state.Pair.ValleyAtUtc,
                    Price = state.ValleyPrice.Value
                });
            }

            if (state.SecondPeakAtUtc.HasValue && state.SecondPeakPrice.HasValue)
            {
                points.Add(new PatternStructuralPoint
                {
                    PointType = "SECOND_PEAK",
                    Timestamp = state.SecondPeakAtUtc.Value,
                    Price = state.SecondPeakPrice.Value
                });
            }

            return points;
        }

        private static object BuildRawPayload(AnalysisRequest request, List<TickerCandle> candles, ExecutedPatternArtifact patternArtifact)
        {
            return new
            {
                engine = "API_DETERMINISTIC",
                engineVersion = AnalysisEngineVersion,
                symbol = request.Instrument.Symbol,
                patternIds = request.ResolvedPatternIds,
                candleInterval = request.CandleInterval,
                historyStartDate = request.HistoryStartDate,
                historyEndDate = request.HistoryEndDate,
                candleCount = candles.Count,
                generatedAtUtc = candles[^1].Date,
                patternAssessment = new
                {
                    patternId = patternArtifact.ContractAssessment.PatternId,
                    phase = patternArtifact.Phase,
                    confidence = patternArtifact.Confidence,
                    currentPrice = patternArtifact.CurrentPrice,
                    necklinePrice = patternArtifact.NecklinePrice,
                    targetPrice = patternArtifact.TargetPrice,
                    invalidationPrice = patternArtifact.InvalidationPrice,
                    firstPeakAtUtc = patternArtifact.FirstPeakAtUtc,
                    secondPeakAtUtc = patternArtifact.SecondPeakAtUtc
                }
            };
        }

        private static decimal ComputeRelativeGap(decimal currentPrice, decimal referencePrice)
        {
            var denominator = Math.Max(referencePrice, 0.000001m);
            return Math.Abs(currentPrice - referencePrice) / denominator;
        }

        private sealed class DoubleTopConfig
        {
            public int PeakWindow { get; } = 3;
            public int MinPeakDistance { get; } = 5;
            public int MaxPeakDistance { get; } = 30;
            public decimal PeakTolerancePct { get; } = 0.02m;
            public decimal ValleyDropPct { get; } = 0.04m;
            public int PretrendLookback { get; } = 10;
            public decimal MinPretrendPct { get; } = 0.05m;
        }

        private sealed class DoubleTopPair
        {
            public int FirstPeakIndex { get; set; }
            public int ValleyIndex { get; set; }
            public int SecondPeakIndex { get; set; }
            public decimal FirstPeakPrice { get; set; }
            public decimal ValleyPrice { get; set; }
            public decimal SecondPeakPrice { get; set; }
            public decimal NecklinePrice { get; set; }
            public decimal InvalidationPrice { get; set; }
            public decimal TargetPrice { get; set; }
            public decimal PretrendMove { get; set; }
            public decimal PeakDiffRatio { get; set; }
            public decimal ValleyDropRatio { get; set; }
            public DateTime ValleyAtUtc { get; set; }
        }

        private sealed class DeterministicDoubleTopState
        {
            public string Phase { get; set; } = string.Empty;
            public decimal CurrentPrice { get; set; }
            public decimal? NecklinePrice { get; set; }
            public decimal? InvalidationPrice { get; set; }
            public decimal? TargetPrice { get; set; }
            public DateTime? FirstPeakAtUtc { get; set; }
            public DateTime? SecondPeakAtUtc { get; set; }
            public decimal? FirstPeakPrice { get; set; }
            public decimal? ValleyPrice { get; set; }
            public decimal? SecondPeakPrice { get; set; }
            public decimal PretrendMove { get; set; }
            public decimal PeakDiffRatio { get; set; }
            public decimal ValleyDropRatio { get; set; }
            public DoubleTopPair? Pair { get; set; }
        }
    }
}
