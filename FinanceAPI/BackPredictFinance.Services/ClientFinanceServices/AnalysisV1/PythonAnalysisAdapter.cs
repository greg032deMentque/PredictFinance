using BackPredictFinance.Common.enums;
using BackPredictFinance.Services.PythonServices;
using BackPredictFinance.Services.PythonServices.Models;
using BackPredictFinance.ViewModels.ClientFinanceViewModels.AnalysisV1;
using System.Globalization;
using System.Text.Json;

namespace BackPredictFinance.Services.ClientFinanceServices.AnalysisV1
{
    public sealed class PythonAnalysisAdapter : IOptionalPythonAnalysisAdapter
    {
        private readonly IPythonApiService _pythonApiService;

        public PythonAnalysisAdapter(IPythonApiService pythonApiService)
        {
            _pythonApiService = pythonApiService;
        }

        public async Task<AnalysisExecutionArtifact> ExecuteAsync(AnalysisRequest request, ResolvedAnalysisPattern pattern, CancellationToken ct = default)
        {
            var prediction = await _pythonApiService.PredictAsync(new AssetIn
            {
                Symbol = request.Instrument.Symbol,
                Pattern = pattern.PatternId
            });

            return new AnalysisExecutionArtifact
            {
                Symbol = prediction.Symbol,
                GeneratedAtUtc = prediction.PredictedAt,
                Patterns = BuildPatternArtifacts(prediction, pattern),
                ModelStatus = prediction.ModelStatus,
                ModelMessage = prediction.ModelMessage,
                ModelVersion = prediction.ModelVersion,
                Precision = prediction.Precision,
                F1 = prediction.F1,
                RocAuc = prediction.RocAuc,
                PositiveSamples = prediction.PositiveSamples,
                SelectedThreshold = prediction.SelectedThreshold,
                RawProviderPayloadJson = JsonSerializer.Serialize(prediction)
            };
        }

        private static List<ExecutedPatternArtifact> BuildPatternArtifacts(PythonPredictionResult prediction, ResolvedAnalysisPattern pattern)
        {
            if (prediction.Patterns.Count > 0)
            {
                return prediction.Patterns
                    .Select(patternPrediction => BuildPatternArtifact(patternPrediction, prediction, pattern))
                    .ToList();
            }

            return
            [
                BuildPatternArtifact(
                    new PatternPrediction
                    {
                        Pattern = prediction.Pattern,
                        Phase = prediction.Phase,
                        Probability = prediction.LastProbability,
                        Confidence = prediction.LastProbability,
                        CurrentPrice = prediction.CurrentPrice,
                        NecklinePrice = prediction.NecklinePrice,
                        TargetPrice = prediction.TargetPrice,
                        InvalidationPrice = prediction.InvalidationPrice,
                        IsPrimary = true
                    },
                    prediction,
                    pattern)
            ];
        }

        private static ExecutedPatternArtifact BuildPatternArtifact(PatternPrediction patternPrediction, PythonPredictionResult prediction, ResolvedAnalysisPattern pattern)
        {
            var normalizedPhase = NormalizePhase(patternPrediction.Phase);
            var confidence = NormalizeConfidence(patternPrediction.Confidence > 0m ? patternPrediction.Confidence : patternPrediction.Probability);
            var status = MapStatus(normalizedPhase);
            var patternId = MapPatternId(patternPrediction.Pattern);

            var assessment = new PatternAssessment
            {
                AssessmentId = Guid.NewGuid().ToString("N"),
                PatternId = patternId,
                DisplayName = GetDisplayName(patternPrediction.Pattern),
                PedagogicalDescription = GetPedagogicalDescription(patternPrediction.Pattern),
                AnalysisWindow = new PatternAnalysisWindow
                {
                    Interval = "1d",
                    StartDate = DateOnly.FromDateTime(prediction.PredictedAt.AddDays(-Math.Max(prediction.NWindows, 1))),
                    EndDate = DateOnly.FromDateTime(prediction.PredictedAt),
                    RequiredCandles = prediction.NWindows,
                    ActualCandles = prediction.NWindows
                },
                Detection = new PatternDetection
                {
                    IsCompatible = confidence > 0m,
                    Status = status,
                    CurrentPhaseCode = normalizedPhase,
                    CurrentPhaseLabel = BuildPhaseLabel(normalizedPhase),
                    StatusReason = BuildStatusReason(status, patternPrediction.CurrentPrice),
                    CurrentPrice = patternPrediction.CurrentPrice,
                    StructuralPoints = BuildStructuralPoints(patternPrediction)
                },
                Validation = new PatternValidation
                {
                    State = status == PatternStatus.Confirmed ? "VALIDATED" : "NOT_VALIDATED",
                    Reason = status == PatternStatus.Confirmed
                        ? "La structure a atteint un etat confirme."
                        : "La structure n'a pas atteint un etat confirme."
                },
                Invalidation = new PatternInvalidation
                {
                    State = status == PatternStatus.Invalidated ? "INVALIDATED" : "ACTIVE",
                    Reason = status == PatternStatus.Invalidated
                        ? "Le scenario est invalide au vu de l'action recente des prix."
                        : "Le scenario reste actif a ce stade.",
                    InvalidationLevel = patternPrediction.InvalidationPrice
                },
                Scoring = new PatternScoring
                {
                    ConfidenceScore = confidence,
                    ConfidenceLabel = BuildConfidenceLabel(confidence),
                    IsCredible = confidence > 0m,
                    ScoreReasons =
                    [
                        $"Confiance calculee a {Math.Round(confidence * 100m, 2).ToString(CultureInfo.InvariantCulture)}%."
                    ],
                    ScoreVersion = pattern.ModelVersion
                },
                RiskHints = new PatternRiskHints(),
                Explanation = new PatternExplanation(),
                Trace = new PatternTrace
                {
                    PatternVersion = pattern.ModelVersion,
                    RuleSetVersion = pattern.ModelVersion,
                    IsPrimaryDisplayCandidate = patternPrediction.IsPrimary,
                    ScoringVersion = pattern.ModelVersion
                }
            };

            return new ExecutedPatternArtifact
            {
                Pattern = patternPrediction.Pattern,
                Phase = normalizedPhase,
                Probability = patternPrediction.Probability,
                Confidence = confidence,
                CurrentPrice = patternPrediction.CurrentPrice,
                NecklinePrice = patternPrediction.NecklinePrice,
                TargetPrice = patternPrediction.TargetPrice,
                InvalidationPrice = patternPrediction.InvalidationPrice,
                FirstPeakAtUtc = patternPrediction.FirstPeakAtUtc,
                SecondPeakAtUtc = patternPrediction.SecondPeakAtUtc,
                IsPrimary = patternPrediction.IsPrimary,
                ContractAssessment = assessment
            };
        }

        private static List<PatternStructuralPoint> BuildStructuralPoints(PatternPrediction patternPrediction)
        {
            var points = new List<PatternStructuralPoint>();
            if (patternPrediction.FirstPeakAtUtc.HasValue)
            {
                points.Add(new PatternStructuralPoint
                {
                    PointType = "FIRST_PEAK",
                    Timestamp = patternPrediction.FirstPeakAtUtc.Value,
                    Price = patternPrediction.CurrentPrice
                });
            }

            if (patternPrediction.SecondPeakAtUtc.HasValue)
            {
                points.Add(new PatternStructuralPoint
                {
                    PointType = "SECOND_PEAK",
                    Timestamp = patternPrediction.SecondPeakAtUtc.Value,
                    Price = patternPrediction.CurrentPrice
                });
            }

            return points;
        }

        private static string NormalizePhase(string? phase)
        {
            return string.IsNullOrWhiteSpace(phase)
                ? "FORMING"
                : phase.Trim().ToUpperInvariant();
        }

        private static PatternStatus MapStatus(string phase)
        {
            return phase switch
            {
                "CONFIRMED" => PatternStatus.Confirmed,
                "INVALIDATED" => PatternStatus.Invalidated,
                "COMPLETED" => PatternStatus.Completed,
                "MONITORING" => PatternStatus.Monitoring,
                _ => PatternStatus.Forming
            };
        }

        private static decimal NormalizeConfidence(decimal confidence)
        {
            if (confidence < 0m)
            {
                return 0m;
            }

            if (confidence > 1m)
            {
                return 1m;
            }

            return confidence;
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

        private static string BuildPhaseLabel(string phase)
        {
            return phase switch
            {
                "CONFIRMED" => "Confirme",
                "INVALIDATED" => "Invalide",
                "COMPLETED" => "Termine",
                "MONITORING" => "Sous surveillance",
                _ => "En formation"
            };
        }

        private static string BuildStatusReason(PatternStatus status, decimal currentPrice)
        {
            return status switch
            {
                PatternStatus.Confirmed => $"La structure reste confirmee autour de {currentPrice.ToString(CultureInfo.InvariantCulture)}.",
                PatternStatus.Invalidated => $"La structure est invalidee autour de {currentPrice.ToString(CultureInfo.InvariantCulture)}.",
                PatternStatus.Completed => $"La structure n'est plus active au prix courant {currentPrice.ToString(CultureInfo.InvariantCulture)}.",
                PatternStatus.Monitoring => $"La structure reste a surveiller autour de {currentPrice.ToString(CultureInfo.InvariantCulture)}.",
                _ => $"La structure reste en formation autour de {currentPrice.ToString(CultureInfo.InvariantCulture)}."
            };
        }

        private static string MapPatternId(TradingPatternEnum pattern)
        {
            return pattern switch
            {
                TradingPatternEnum.HeadAndShoulders => "HEAD_AND_SHOULDERS",
                TradingPatternEnum.DoubleTop => "DOUBLE_TOP",
                TradingPatternEnum.DoubleBottom => "DOUBLE_BOTTOM",
                TradingPatternEnum.CupAndHandle => "CUP_AND_HANDLE",
                TradingPatternEnum.Triangle => "TRIANGLE",
                _ => "DOUBLE_TOP"
            };
        }

        private static string GetDisplayName(TradingPatternEnum pattern)
        {
            return pattern switch
            {
                TradingPatternEnum.HeadAndShoulders => "Epaule-tete-epaule",
                TradingPatternEnum.DoubleTop => "Double sommet",
                TradingPatternEnum.DoubleBottom => "Double creux",
                TradingPatternEnum.CupAndHandle => "Tasse avec anse",
                TradingPatternEnum.Triangle => "Triangle",
                _ => "Double sommet"
            };
        }

        private static string GetPedagogicalDescription(TradingPatternEnum pattern)
        {
            return pattern switch
            {
                TradingPatternEnum.HeadAndShoulders => "Schema de retournement avec trois sommets successifs.",
                TradingPatternEnum.DoubleTop => "Schema de retournement baissier avec deux sommets proches.",
                TradingPatternEnum.DoubleBottom => "Schema de retournement haussier avec deux creux proches.",
                TradingPatternEnum.CupAndHandle => "Schema de continuation compose d'une tasse puis d'une anse.",
                TradingPatternEnum.Triangle => "Schema de compression ou les prix convergent avant rupture.",
                _ => "Schema de retournement avec deux sommets proches."
            };
        }
    }
}
