using System.Text.Json;
using BackPredictFinance.Common.enums;
using BackPredictFinance.Contracts.Analysis;
using BackPredictFinance.Contracts.MarketData;
using BackPredictFinance.Services.TwelveDataServices;

namespace BackPredictFinance.Services.ClientFinanceServices.Analysis
{
    internal static class ContinuationPatternIds
    {
        public const string RectangleContinuation = "RECTANGLE_CONTINUATION";
        public const string SymmetricalTriangleContinuation = "SYMMETRICAL_TRIANGLE_CONTINUATION";
        public const string BullFlagContinuation = "BULL_FLAG_CONTINUATION";
        public const string BearFlagContinuation = "BEAR_FLAG_CONTINUATION";
    }

    internal static class ContinuationPatternFamilies
    {
        public const string HorizontalBreakout = "HORIZONTAL_BREAKOUT";
        public const string TriangleContinuation = "TRIANGLE_CONTINUATION";
        public const string PoleContinuation = "POLE_CONTINUATION";
    }

    public sealed class PatternComputationResult
    {
        public string PhaseCode { get; set; } = string.Empty;
        public PatternStatusEnum Status { get; set; }
        public bool IsCompatible { get; set; }
        public string StatusReason { get; set; } = string.Empty;
        public string ValidationState { get; set; } = string.Empty;
        public string ValidationReason { get; set; } = string.Empty;
        public string? ValidationRuleCode { get; set; }
        public string InvalidationState { get; set; } = "ACTIVE";
        public string InvalidationReason { get; set; } = string.Empty;
        public string? InvalidationRuleCode { get; set; }
        public decimal CurrentPrice { get; set; }
        public decimal? BreakoutPrice { get; set; }
        public decimal? InvalidationLevel { get; set; }
        public decimal? TargetPrice { get; set; }
        public decimal ConfidenceScore { get; set; }
        public List<string> ScoreReasons { get; set; } = [];
        public List<PatternStructuralPoint> StructuralPoints { get; set; } = [];
    }

    public abstract class ContinuationPatternDefinitionBase : IAnalysisPatternDefinition
    {
        private readonly ITickerService _tickerService;

        protected ContinuationPatternDefinitionBase(ITickerService tickerService)
        {
            _tickerService = tickerService;
        }

        public abstract string PatternId { get; }
        public abstract string DisplayName { get; }
        public abstract string FamilyId { get; }
        public abstract string BiasCode { get; }
        public abstract string ModelVersion { get; }
        public abstract int HistoryLookbackMonths { get; }
        public abstract int MinimumRequiredCandles { get; }
        protected abstract string PedagogicalDescription { get; }
        protected abstract TradingPatternEnum TradingPattern { get; }
        protected abstract PatternComputationResult Analyze(List<TickerCandle> candles, AnalysisRequest request);

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

        public async Task<AnalysisExecutionArtifact> ExecuteAsync(AnalysisRequest request, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(request);
            var candles = (await _tickerService.GetTimeSeriesAsync(
                    request.Instrument.Symbol,
                    string.IsNullOrWhiteSpace(request.CandleInterval) ? "1d" : request.CandleInterval.Trim(),
                    Math.Clamp(Math.Max(MinimumRequiredCandles, request.HistoryEndDate.DayNumber - request.HistoryStartDate.DayNumber + 1), MinimumRequiredCandles, 500),
                    ct))
                .Candles
                .OrderBy(x => x.Date)
                .ToList();

            if (candles.Count == 0)
            {
                throw new InvalidOperationException("Aucune donnee de marche exploitable n'a ete retournee pour l'analyse.");
            }

            var result = Analyze(candles, request);
            var assessmentId = Guid.NewGuid().ToString("N");
            DateOnly? validatedDate = result.ValidationState == "VALIDATED" && result.BreakoutPrice.HasValue ? DateOnly.FromDateTime(candles[^1].Date) : null;
            DateOnly? invalidatedDate = result.InvalidationState == "INVALIDATED" ? DateOnly.FromDateTime(candles[^1].Date) : null;
            var assessment = new PatternAssessment
            {
                AssessmentId = assessmentId,
                PatternId = PatternId,
                DisplayName = DisplayName,
                FamilyId = FamilyId,
                BiasCode = BiasCode,
                PedagogicalDescription = PedagogicalDescription,
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
                    IsCompatible = result.IsCompatible,
                    Status = result.Status,
                    CurrentPhaseCode = result.PhaseCode,
                    CurrentPhaseLabel = ContinuationPatternMath.BuildPhaseLabel(result.PhaseCode),
                    StatusReason = result.StatusReason,
                    CurrentPrice = result.CurrentPrice,
                    StructuralPoints = result.StructuralPoints
                },
                Validation = new PatternValidation
                {
                    State = result.ValidationState,
                    Reason = result.ValidationReason,
                    ValidatedAtDate = validatedDate,
                    ValidatedAtPrice = result.BreakoutPrice,
                    ValidationRuleCode = result.ValidationRuleCode
                },
                Invalidation = new PatternInvalidation
                {
                    State = result.InvalidationState,
                    Reason = result.InvalidationReason,
                    InvalidationLevel = result.InvalidationLevel,
                    BreachedAtDate = invalidatedDate,
                    BreachedAtPrice = result.InvalidationState == "INVALIDATED" ? result.CurrentPrice : null,
                    InvalidationRuleCode = result.InvalidationRuleCode
                },
                Scoring = new PatternScoring
                {
                    ConfidenceScore = result.ConfidenceScore,
                    ConfidenceLabel = ContinuationPatternMath.BuildConfidenceLabel(result.ConfidenceScore),
                    IsCredible = result.IsCompatible,
                    ScoreReasons = result.ScoreReasons,
                    ScoreVersion = ModelVersion
                },
                RiskHints = new PatternRiskHints(),
                Explanation = new PatternExplanation(),
                Trace = new PatternTrace
                {
                    PatternVersion = ModelVersion,
                    RuleSetVersion = ModelVersion,
                    IsPrimaryDisplayCandidate = result.IsCompatible,
                    ScoringVersion = ModelVersion
                }
            };

            return new AnalysisExecutionArtifact
            {
                Symbol = request.Instrument.Symbol,
                GeneratedAtUtc = candles[^1].Date,
                Patterns = [new ExecutedPatternArtifact
                {
                    Pattern = TradingPattern,
                    Phase = result.PhaseCode,
                    Probability = result.ConfidenceScore,
                    Confidence = result.ConfidenceScore,
                    CurrentPrice = result.CurrentPrice,
                    NecklinePrice = result.BreakoutPrice,
                    TargetPrice = result.TargetPrice,
                    InvalidationPrice = result.InvalidationLevel,
                    IsPrimary = result.IsCompatible,
                    ContractAssessment = assessment
                }],
                ModelStatus = ModelStatusEnum.Go,
                ModelMessage = "Analyse V1 produite par le moteur deterministe API.",
                ModelVersion = ModelVersion,
                RawProviderPayloadJson = JsonSerializer.Serialize(new { patternId = PatternId, generatedAtUtc = candles[^1].Date, candleCount = candles.Count })
            };
        }
    }

    internal static class ContinuationPatternMath
    {
        public static string BuildPhaseLabel(string phaseCode)
        {
            return phaseCode switch
            {
                "FORMING" => "Formation",
                "MONITORING" => "Surveillance",
                "CONFIRMED" => "Confirme",
                "INVALIDATED" => "Invalide",
                _ => "Observation"
            };
        }

        public static string BuildConfidenceLabel(decimal confidence)
        {
            return confidence switch
            {
                >= 0.75m => "ELEVEE",
                >= 0.50m => "MODEREE",
                >= 0.30m => "FAIBLE",
                _ => "TRES_FAIBLE"
            };
        }

        public static decimal RelativeGap(decimal left, decimal right)
        {
            if (left == 0m && right == 0m)
            {
                return 0m;
            }

            var denominator = Math.Max(Math.Abs(left), Math.Abs(right));
            return denominator == 0m ? 0m : Math.Abs(left - right) / denominator;
        }

        public static decimal Average(IEnumerable<decimal> values)
        {
            var list = values.ToList();
            return list.Count == 0 ? 0m : list.Average();
        }

        public static decimal AverageVolume(IEnumerable<TickerCandle> candles)
        {
            var list = candles.ToList();
            return list.Count == 0 ? 0m : list.Average(x => x.Volume);
        }

        public static string DeterminePriorTrend(List<TickerCandle> candles, int priorStartIndex, int priorEndIndex, decimal threshold)
        {
            if (priorStartIndex < 0 || priorEndIndex <= priorStartIndex || priorEndIndex >= candles.Count)
            {
                return "NONE";
            }

            var start = candles[priorStartIndex].Close;
            var end = candles[priorEndIndex].Close;
            if (start <= 0m)
            {
                return "NONE";
            }

            var change = (end - start) / start;
            if (change >= threshold)
            {
                return "BULLISH";
            }

            if (change <= -threshold)
            {
                return "BEARISH";
            }

            return "NONE";
        }

        public static bool BreakoutAbove(decimal currentPrice, decimal resistance)
        {
            return currentPrice > resistance * 1.002m;
        }

        public static bool BreakoutBelow(decimal currentPrice, decimal support)
        {
            return currentPrice < support * 0.998m;
        }

        public static decimal Clamp(decimal value)
        {
            if (value < 0m)
            {
                return 0m;
            }

            if (value > 0.98m)
            {
                return 0.98m;
            }

            return Math.Round(value, 4);
        }

        public static List<int> FindLocalHighs(List<TickerCandle> candles, int startIndex, int endIndex, int window)
        {
            var highs = new List<int>();
            for (var index = Math.Max(startIndex + window, window); index <= Math.Min(endIndex - window, candles.Count - 1 - window); index++)
            {
                var current = candles[index].High;
                var isHigh = true;
                for (var offset = 1; offset <= window; offset++)
                {
                    if (candles[index - offset].High >= current || candles[index + offset].High > current)
                    {
                        isHigh = false;
                        break;
                    }
                }

                if (isHigh)
                {
                    highs.Add(index);
                }
            }

            return highs;
        }

        public static List<int> FindLocalLows(List<TickerCandle> candles, int startIndex, int endIndex, int window)
        {
            var lows = new List<int>();
            for (var index = Math.Max(startIndex + window, window); index <= Math.Min(endIndex - window, candles.Count - 1 - window); index++)
            {
                var current = candles[index].Low;
                var isLow = true;
                for (var offset = 1; offset <= window; offset++)
                {
                    if (candles[index - offset].Low <= current || candles[index + offset].Low < current)
                    {
                        isLow = false;
                        break;
                    }
                }

                if (isLow)
                {
                    lows.Add(index);
                }
            }

            return lows;
        }

        public static decimal SlopePerBar(int firstIndex, decimal firstValue, int lastIndex, decimal lastValue)
        {
            var bars = Math.Max(1, lastIndex - firstIndex);
            return (lastValue - firstValue) / bars;
        }
    }

    public sealed class RectangleContinuationPatternDefinition : ContinuationPatternDefinitionBase
    {
        public RectangleContinuationPatternDefinition(ITickerService tickerService) : base(tickerService) { }
        public override string PatternId => ContinuationPatternIds.RectangleContinuation;
        public override string DisplayName => "Rectangle continuation";
        public override string FamilyId => ContinuationPatternFamilies.HorizontalBreakout;
        public override string BiasCode => "NEUTRAL_CONTINUATION";
        public override string ModelVersion => "analysis-v1-rectangle-continuation@prompt10";
        public override int HistoryLookbackMonths => 6;
        public override int MinimumRequiredCandles => 50;
        protected override string PedagogicalDescription => "Consolidation horizontale de continuation avec bornes paralleles et cassure dans le sens de la tendance precedente.";
        protected override TradingPatternEnum TradingPattern => TradingPatternEnum.RectangleContinuation;

        protected override PatternComputationResult Analyze(List<TickerCandle> candles, AnalysisRequest request)
        {
            var current = candles[^1];
            var structureLength = Math.Min(22, candles.Count - 16);
            var structureStart = candles.Count - structureLength;
            var priorStart = Math.Max(0, structureStart - 16);
            var priorEnd = structureStart - 1;
            var priorTrend = ContinuationPatternMath.DeterminePriorTrend(candles, priorStart, priorEnd, 0.06m);
            var structure = candles.Skip(structureStart).Take(structureLength).ToList();
            var resistance = structure.Max(x => x.High);
            var support = structure.Min(x => x.Low);
            var mid = (resistance + support) / 2m;
            var rangePct = mid == 0m ? 0m : (resistance - support) / mid;
            var highs = ContinuationPatternMath.FindLocalHighs(candles, structureStart, candles.Count - 2, 1);
            var lows = ContinuationPatternMath.FindLocalLows(candles, structureStart, candles.Count - 2, 1);
            var resistanceTests = highs.Count(i => ContinuationPatternMath.RelativeGap(candles[i].High, resistance) <= 0.015m);
            var supportTests = lows.Count(i => ContinuationPatternMath.RelativeGap(candles[i].Low, support) <= 0.015m);
            var firstHighIndex = highs.FirstOrDefault();
            var lastHighIndex = highs.LastOrDefault();
            var firstLowIndex = lows.FirstOrDefault();
            var lastLowIndex = lows.LastOrDefault();
            var resistanceSlope = highs.Count >= 2 ? ContinuationPatternMath.SlopePerBar(firstHighIndex, candles[firstHighIndex].High, lastHighIndex, candles[lastHighIndex].High) : 0m;
            var supportSlope = lows.Count >= 2 ? ContinuationPatternMath.SlopePerBar(firstLowIndex, candles[firstLowIndex].Low, lastLowIndex, candles[lastLowIndex].Low) : 0m;
            var nearHorizontal = Math.Abs(resistanceSlope) <= mid * 0.001m && Math.Abs(supportSlope) <= mid * 0.001m;
            var breakoutUp = ContinuationPatternMath.BreakoutAbove(current.Close, resistance);
            var breakoutDown = ContinuationPatternMath.BreakoutBelow(current.Close, support);
            var reintegrated = structureLength >= 3 && (candles[^2].Close < resistance && candles[^2].Close > support) && (breakoutUp || breakoutDown);
            var confirmedDirection = priorTrend == "BULLISH" ? breakoutUp : priorTrend == "BEARISH" ? breakoutDown : false;
            var oppositeBreakout = priorTrend == "BULLISH" ? breakoutDown : priorTrend == "BEARISH" ? breakoutUp : breakoutUp || breakoutDown;
            var confidence = 0.35m;
            if (priorTrend != "NONE") confidence += 0.12m;
            if (nearHorizontal) confidence += 0.08m;
            if (resistanceTests >= 2 && supportTests >= 2) confidence += 0.08m;
            if (rangePct <= 0.10m) confidence += 0.06m;
            if (confirmedDirection) confidence += 0.10m;
            if (ContinuationPatternMath.AverageVolume(candles.Skip(Math.Max(0, candles.Count - 6)).Take(5)) > 0m && current.Volume > ContinuationPatternMath.AverageVolume(structure) * 1.15m) confidence += 0.04m;
            if (reintegrated) confidence -= 0.10m;
            if (oppositeBreakout) confidence -= 0.25m;
            var height = resistance - support;
            return new PatternComputationResult
            {
                PhaseCode = oppositeBreakout ? "INVALIDATED" : confirmedDirection ? "CONFIRMED" : "FORMING",
                Status = oppositeBreakout ? PatternStatusEnum.Invalidated : confirmedDirection ? PatternStatusEnum.Confirmed : PatternStatusEnum.Forming,
                IsCompatible = priorTrend != "NONE" && nearHorizontal && resistanceTests >= 2 && supportTests >= 2 && rangePct <= 0.10m && !oppositeBreakout,
                StatusReason = oppositeBreakout ? "La cassure opposee a la tendance precedente ne valide pas la continuation." : confirmedDirection ? "Le rectangle casse dans le sens de la tendance precedente." : "Le rectangle reste en formation sans cassure validee dans le sens attendu.",
                ValidationState = confirmedDirection ? "VALIDATED" : "NOT_VALIDATED",
                ValidationReason = confirmedDirection ? "Cassure du rectangle dans le sens de la tendance precedente." : "Aucune cassure validee dans le sens de la tendance precedente.",
                ValidationRuleCode = confirmedDirection ? "RECTANGLE_CONTINUATION_BREAKOUT" : null,
                InvalidationState = oppositeBreakout ? "INVALIDATED" : "ACTIVE",
                InvalidationReason = oppositeBreakout ? "Cassure opposee ou reintegration rapide incompatible avec une continuation propre." : "Le scenario reste actif tant que le support n'est pas cede a contre-sens.",
                InvalidationRuleCode = oppositeBreakout ? "RECTANGLE_CONTINUATION_OPPOSITE_BREAKOUT" : null,
                CurrentPrice = current.Close,
                BreakoutPrice = confirmedDirection || oppositeBreakout ? current.Close : null,
                InvalidationLevel = priorTrend == "BULLISH" ? support : resistance,
                TargetPrice = confirmedDirection ? (priorTrend == "BULLISH" ? current.Close + height : current.Close - height) : null,
                ConfidenceScore = ContinuationPatternMath.Clamp(confidence),
                ScoreReasons = new List<string> { $"prior_trend={priorTrend}", $"resistance_tests={resistanceTests}", $"support_tests={supportTests}", $"range_pct={Math.Round(rangePct, 4)}" },
                StructuralPoints = new List<PatternStructuralPoint>
                {
                    new() { PointType = "RESISTANCE", Timestamp = structure[0].Date, Price = resistance },
                    new() { PointType = "SUPPORT", Timestamp = structure[0].Date, Price = support }
                }
            };
        }
    }

    public sealed class SymmetricalTriangleContinuationPatternDefinition : ContinuationPatternDefinitionBase
    {
        public SymmetricalTriangleContinuationPatternDefinition(ITickerService tickerService) : base(tickerService) { }
        public override string PatternId => ContinuationPatternIds.SymmetricalTriangleContinuation;
        public override string DisplayName => "Symmetrical triangle continuation";
        public override string FamilyId => ContinuationPatternFamilies.TriangleContinuation;
        public override string BiasCode => "NEUTRAL_CONTINUATION";
        public override string ModelVersion => "analysis-v1-symmetrical-triangle@prompt10";
        public override int HistoryLookbackMonths => 6;
        public override int MinimumRequiredCandles => 55;
        protected override string PedagogicalDescription => "Compression triangulaire de continuation avec sommets descendants et creux ascendants avant cassure dans le sens de la tendance precedente.";
        protected override TradingPatternEnum TradingPattern => TradingPatternEnum.SymmetricalTriangleContinuation;

        protected override PatternComputationResult Analyze(List<TickerCandle> candles, AnalysisRequest request)
        {
            var current = candles[^1];
            var structureLength = Math.Min(26, candles.Count - 18);
            var structureStart = candles.Count - structureLength;
            var priorTrend = ContinuationPatternMath.DeterminePriorTrend(candles, Math.Max(0, structureStart - 18), structureStart - 1, 0.07m);
            var highs = ContinuationPatternMath.FindLocalHighs(candles, structureStart, candles.Count - 2, 1);
            var lows = ContinuationPatternMath.FindLocalLows(candles, structureStart, candles.Count - 2, 1);
            var descendingHighs = highs.Count >= 2 && candles[highs[0]].High > candles[highs[^1]].High;
            var ascendingLows = lows.Count >= 2 && candles[lows[0]].Low < candles[lows[^1]].Low;
            var upperSlope = highs.Count >= 2 ? ContinuationPatternMath.SlopePerBar(highs[0], candles[highs[0]].High, highs[^1], candles[highs[^1]].High) : 0m;
            var lowerSlope = lows.Count >= 2 ? ContinuationPatternMath.SlopePerBar(lows[0], candles[lows[0]].Low, lows[^1], candles[lows[^1]].Low) : 0m;
            var startWidth = highs.Count >= 1 && lows.Count >= 1 ? candles[highs[0]].High - candles[lows[0]].Low : 0m;
            var endWidth = highs.Count >= 1 && lows.Count >= 1 ? candles[highs[^1]].High - candles[lows[^1]].Low : startWidth;
            var compressed = startWidth > 0m && endWidth / startWidth <= 0.70m;
            var upperBoundary = highs.Count >= 2 ? candles[highs[^1]].High : candles.Skip(structureStart).Max(x => x.High);
            var lowerBoundary = lows.Count >= 2 ? candles[lows[^1]].Low : candles.Skip(structureStart).Min(x => x.Low);
            var breakoutUp = ContinuationPatternMath.BreakoutAbove(current.Close, upperBoundary);
            var breakoutDown = ContinuationPatternMath.BreakoutBelow(current.Close, lowerBoundary);
            var confirmedDirection = priorTrend == "BULLISH" ? breakoutUp : priorTrend == "BEARISH" ? breakoutDown : false;
            var oppositeBreakout = priorTrend == "BULLISH" ? breakoutDown : priorTrend == "BEARISH" ? breakoutUp : breakoutUp || breakoutDown;
            var confidence = 0.34m;
            if (priorTrend != "NONE") confidence += 0.12m;
            if (descendingHighs && ascendingLows) confidence += 0.10m;
            if (compressed) confidence += 0.08m;
            if (confirmedDirection) confidence += 0.10m;
            if (current.Volume > ContinuationPatternMath.AverageVolume(candles.Skip(Math.Max(0, candles.Count - 8)).Take(7)) * 1.20m) confidence += 0.04m;
            if (oppositeBreakout) confidence -= 0.25m;
            var height = startWidth;
            return new PatternComputationResult
            {
                PhaseCode = oppositeBreakout ? "INVALIDATED" : confirmedDirection ? "CONFIRMED" : "FORMING",
                Status = oppositeBreakout ? PatternStatusEnum.Invalidated : confirmedDirection ? PatternStatusEnum.Confirmed : PatternStatusEnum.Forming,
                IsCompatible = priorTrend != "NONE" && descendingHighs && ascendingLows && compressed && !oppositeBreakout,
                StatusReason = oppositeBreakout ? "La cassure opposee ne valide pas la continuation triangulaire." : confirmedDirection ? "Le triangle casse dans le sens de la tendance precedente." : "Le triangle reste en compression sans cassure validee.",
                ValidationState = confirmedDirection ? "VALIDATED" : "NOT_VALIDATED",
                ValidationReason = confirmedDirection ? "Cassure du triangle dans le sens de la tendance precedente." : "Aucune cassure validee du triangle dans le sens attendu.",
                ValidationRuleCode = confirmedDirection ? "SYMMETRICAL_TRIANGLE_CONTINUATION_BREAKOUT" : null,
                InvalidationState = oppositeBreakout ? "INVALIDATED" : "ACTIVE",
                InvalidationReason = oppositeBreakout ? "Cassure opposee ou reintegration rapide incompatible avec une continuation triangulaire." : "Le scenario reste actif tant que la borne opposee n'est pas cede a contre-sens.",
                InvalidationRuleCode = oppositeBreakout ? "SYMMETRICAL_TRIANGLE_CONTINUATION_OPPOSITE_BREAKOUT" : null,
                CurrentPrice = current.Close,
                BreakoutPrice = confirmedDirection || oppositeBreakout ? current.Close : null,
                InvalidationLevel = priorTrend == "BULLISH" ? lowerBoundary : upperBoundary,
                TargetPrice = confirmedDirection ? (priorTrend == "BULLISH" ? current.Close + height : current.Close - height) : null,
                ConfidenceScore = ContinuationPatternMath.Clamp(confidence),
                ScoreReasons = new List<string> { $"prior_trend={priorTrend}", $"upper_slope={Math.Round(upperSlope, 6)}", $"lower_slope={Math.Round(lowerSlope, 6)}", $"compression_ratio={Math.Round(startWidth == 0m ? 0m : endWidth / startWidth, 4)}" },
                StructuralPoints = new List<PatternStructuralPoint>
                {
                    new() { PointType = "UPPER_BOUNDARY", Timestamp = candles[Math.Max(structureStart, 0)].Date, Price = upperBoundary },
                    new() { PointType = "LOWER_BOUNDARY", Timestamp = candles[Math.Max(structureStart, 0)].Date, Price = lowerBoundary }
                }
            };
        }
    }

    public sealed class BullFlagContinuationPatternDefinition : ContinuationPatternDefinitionBase
    {
        public BullFlagContinuationPatternDefinition(ITickerService tickerService) : base(tickerService) { }
        public override string PatternId => ContinuationPatternIds.BullFlagContinuation;
        public override string DisplayName => "Bull flag continuation";
        public override string FamilyId => ContinuationPatternFamilies.PoleContinuation;
        public override string BiasCode => "BULLISH";
        public override string ModelVersion => "analysis-v1-bull-flag@prompt10";
        public override int HistoryLookbackMonths => 6;
        public override int MinimumRequiredCandles => 40;
        protected override string PedagogicalDescription => "Acceleration haussiere suivie d'une consolidation courte avant reprise theorique dans le sens haussier.";
        protected override TradingPatternEnum TradingPattern => TradingPatternEnum.BullFlagContinuation;

        protected override PatternComputationResult Analyze(List<TickerCandle> candles, AnalysisRequest request)
        {
            var current = candles[^1];
            var flagLength = 10;
            var poleEnd = candles.Count - flagLength - 1;
            var poleStart = Math.Max(0, poleEnd - 8);
            var poleLow = candles.Skip(poleStart).Take(poleEnd - poleStart + 1).Min(x => x.Low);
            var poleHigh = candles.Skip(poleStart).Take(poleEnd - poleStart + 1).Max(x => x.High);
            var poleHeight = poleHigh - poleLow;
            var poleStrength = poleLow <= 0m ? 0m : poleHeight / poleLow;
            var flag = candles.Skip(poleEnd + 1).ToList();
            var flagHigh = flag.Max(x => x.High);
            var flagLow = flag.Min(x => x.Low);
            var flagSlope = (flag[^1].Close - flag[0].Close) / Math.Max(1, flag.Count - 1);
            var tightFlag = poleHeight > 0m && (flagHigh - flagLow) / poleHeight <= 0.55m;
            var acceptableSlope = flagSlope <= current.Close * 0.0025m;
            var breakoutUp = ContinuationPatternMath.BreakoutAbove(current.Close, flagHigh);
            var breakdown = ContinuationPatternMath.BreakoutBelow(current.Close, flagLow);
            var confidence = 0.36m;
            if (poleStrength >= 0.08m) confidence += 0.15m;
            if (tightFlag) confidence += 0.10m;
            if (acceptableSlope) confidence += 0.08m;
            if (breakoutUp) confidence += 0.11m;
            if (current.Volume > ContinuationPatternMath.AverageVolume(flag.Take(Math.Max(1, flag.Count - 1))) * 1.20m) confidence += 0.04m;
            if (breakdown) confidence -= 0.30m;
            return new PatternComputationResult
            {
                PhaseCode = breakdown ? "INVALIDATED" : breakoutUp ? "CONFIRMED" : "FORMING",
                Status = breakdown ? PatternStatusEnum.Invalidated : breakoutUp ? PatternStatusEnum.Confirmed : PatternStatusEnum.Forming,
                IsCompatible = poleStrength >= 0.08m && tightFlag && acceptableSlope && !breakdown,
                StatusReason = breakdown ? "La consolidation cede a contre-sens et invalide le bull flag." : breakoutUp ? "La sortie haussiere du flag valide la continuation." : "Le bull flag reste en consolidation courte sous la borne haute.",
                ValidationState = breakoutUp ? "VALIDATED" : "NOT_VALIDATED",
                ValidationReason = breakoutUp ? "Cassure haussiere du flag apres pole haussier." : "Aucune cassure haussiere validee du flag.",
                ValidationRuleCode = breakoutUp ? "BULL_FLAG_BREAKOUT" : null,
                InvalidationState = breakdown ? "INVALIDATED" : "ACTIVE",
                InvalidationReason = breakdown ? "La borne basse du flag est cede a contre-sens." : "Le scenario reste actif tant que la borne basse du flag n'est pas rompue.",
                InvalidationRuleCode = breakdown ? "BULL_FLAG_BREAKDOWN" : null,
                CurrentPrice = current.Close,
                BreakoutPrice = breakoutUp || breakdown ? current.Close : null,
                InvalidationLevel = flagLow,
                TargetPrice = breakoutUp ? current.Close + poleHeight : null,
                ConfidenceScore = ContinuationPatternMath.Clamp(confidence),
                ScoreReasons = new List<string> { $"pole_strength={Math.Round(poleStrength, 4)}", $"flag_range_ratio={Math.Round(poleHeight == 0m ? 0m : (flagHigh - flagLow) / poleHeight, 4)}", $"flag_slope={Math.Round(flagSlope, 6)}" },
                StructuralPoints = new List<PatternStructuralPoint>
                {
                    new() { PointType = "POLE_LOW", Timestamp = candles[poleStart].Date, Price = poleLow },
                    new() { PointType = "POLE_HIGH", Timestamp = candles[poleEnd].Date, Price = poleHigh },
                    new() { PointType = "FLAG_LOW", Timestamp = flag[0].Date, Price = flagLow },
                    new() { PointType = "FLAG_HIGH", Timestamp = flag[0].Date, Price = flagHigh }
                }
            };
        }
    }

    public sealed class BearFlagContinuationPatternDefinition : ContinuationPatternDefinitionBase
    {
        public BearFlagContinuationPatternDefinition(ITickerService tickerService) : base(tickerService) { }
        public override string PatternId => ContinuationPatternIds.BearFlagContinuation;
        public override string DisplayName => "Bear flag continuation";
        public override string FamilyId => ContinuationPatternFamilies.PoleContinuation;
        public override string BiasCode => "BEARISH";
        public override string ModelVersion => "analysis-v1-bear-flag@prompt10";
        public override int HistoryLookbackMonths => 6;
        public override int MinimumRequiredCandles => 40;
        protected override string PedagogicalDescription => "Acceleration baissiere suivie d'une consolidation courte avant reprise theorique dans le sens baissier.";
        protected override TradingPatternEnum TradingPattern => TradingPatternEnum.BearFlagContinuation;

        protected override PatternComputationResult Analyze(List<TickerCandle> candles, AnalysisRequest request)
        {
            var current = candles[^1];
            var flagLength = 10;
            var poleEnd = candles.Count - flagLength - 1;
            var poleStart = Math.Max(0, poleEnd - 8);
            var poleHigh = candles.Skip(poleStart).Take(poleEnd - poleStart + 1).Max(x => x.High);
            var poleLow = candles.Skip(poleStart).Take(poleEnd - poleStart + 1).Min(x => x.Low);
            var poleHeight = poleHigh - poleLow;
            var poleStrength = poleHigh <= 0m ? 0m : poleHeight / poleHigh;
            var flag = candles.Skip(poleEnd + 1).ToList();
            var flagHigh = flag.Max(x => x.High);
            var flagLow = flag.Min(x => x.Low);
            var flagSlope = (flag[^1].Close - flag[0].Close) / Math.Max(1, flag.Count - 1);
            var tightFlag = poleHeight > 0m && (flagHigh - flagLow) / poleHeight <= 0.55m;
            var acceptableSlope = flagSlope >= -current.Close * 0.0025m;
            var breakoutDown = ContinuationPatternMath.BreakoutBelow(current.Close, flagLow);
            var breakoutUp = ContinuationPatternMath.BreakoutAbove(current.Close, flagHigh);
            var confidence = 0.36m;
            if (poleStrength >= 0.08m) confidence += 0.15m;
            if (tightFlag) confidence += 0.10m;
            if (acceptableSlope) confidence += 0.08m;
            if (breakoutDown) confidence += 0.11m;
            if (current.Volume > ContinuationPatternMath.AverageVolume(flag.Take(Math.Max(1, flag.Count - 1))) * 1.20m) confidence += 0.04m;
            if (breakoutUp) confidence -= 0.30m;
            return new PatternComputationResult
            {
                PhaseCode = breakoutUp ? "INVALIDATED" : breakoutDown ? "CONFIRMED" : "FORMING",
                Status = breakoutUp ? PatternStatusEnum.Invalidated : breakoutDown ? PatternStatusEnum.Confirmed : PatternStatusEnum.Forming,
                IsCompatible = poleStrength >= 0.08m && tightFlag && acceptableSlope && !breakoutUp,
                StatusReason = breakoutUp ? "La sortie haussiere invalide le bear flag." : breakoutDown ? "La sortie baissiere du flag valide la continuation." : "Le bear flag reste en consolidation courte au-dessus de sa borne basse.",
                ValidationState = breakoutDown ? "VALIDATED" : "NOT_VALIDATED",
                ValidationReason = breakoutDown ? "Cassure baissiere du flag apres pole baissier." : "Aucune cassure baissiere validee du flag.",
                ValidationRuleCode = breakoutDown ? "BEAR_FLAG_BREAKDOWN" : null,
                InvalidationState = breakoutUp ? "INVALIDATED" : "ACTIVE",
                InvalidationReason = breakoutUp ? "La borne haute du flag est rompue a contre-sens." : "Le scenario reste actif tant que la borne haute du flag n'est pas depassee.",
                InvalidationRuleCode = breakoutUp ? "BEAR_FLAG_BREAKOUT_FAILURE" : null,
                CurrentPrice = current.Close,
                BreakoutPrice = breakoutDown || breakoutUp ? current.Close : null,
                InvalidationLevel = flagHigh,
                TargetPrice = breakoutDown ? current.Close - poleHeight : null,
                ConfidenceScore = ContinuationPatternMath.Clamp(confidence),
                ScoreReasons = new List<string> { $"pole_strength={Math.Round(poleStrength, 4)}", $"flag_range_ratio={Math.Round(poleHeight == 0m ? 0m : (flagHigh - flagLow) / poleHeight, 4)}", $"flag_slope={Math.Round(flagSlope, 6)}" },
                StructuralPoints = new List<PatternStructuralPoint>
                {
                    new() { PointType = "POLE_HIGH", Timestamp = candles[poleStart].Date, Price = poleHigh },
                    new() { PointType = "POLE_LOW", Timestamp = candles[poleEnd].Date, Price = poleLow },
                    new() { PointType = "FLAG_LOW", Timestamp = flag[0].Date, Price = flagLow },
                    new() { PointType = "FLAG_HIGH", Timestamp = flag[0].Date, Price = flagHigh }
                }
            };
        }
    }
}
