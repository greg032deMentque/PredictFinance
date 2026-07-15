using BackPredictFinance.Common.AnalysisV1;
using BackPredictFinance.Common.enums;
using BackPredictFinance.Common.MarketData;
using BackPredictFinance.Patterns.Common;
using BackPredictFinance.Services.ClientFinanceServices.Analysis;
using BackPredictFinance.ViewModels.ClientFinanceViewModels.Analysis;
using BackPredictFinance.ViewModels.ClientFinanceViewModels.Patterns;
using Microsoft.EntityFrameworkCore;

namespace BackPredictFinance.Services.ClientFinanceServices.Patterns
{
    public interface IPatternExplorerService
    {
        Task<PatternEvaluateResultViewModel> EvaluateAsync(PatternEvaluateRequestViewModel request, CancellationToken ct = default);
        Task<PatternDetailViewModel?> GetPatternDetailAsync(string analysisId, string patternId, bool holdsInstrument, CancellationToken ct = default);
        Task<List<PatternCatalogViewModel>> GetPatternCatalogAsync(CancellationToken ct = default);
        Task<List<AnalysisConceptViewModel>> GetAnalysisConceptsAsync(CancellationToken ct = default);
    }

    public sealed class PatternExplorerService : BaseService, IPatternExplorerService
    {
        private readonly IClientFinanceAssetSupportService _assetSupportService;
        private readonly IAnalysisRequestCompatibilityResolver _resolver;
        private readonly IAnalysisOrchestrator _orchestrator;
        private readonly IClientFinanceProjectionService _projectionService;
        private readonly IConfidenceBreakdownAssembler _confidenceBreakdownAssembler;
        private readonly IPatternScenarioBranchGenerator _branchGenerator;

        public PatternExplorerService(
            IServiceProvider serviceProvider,
            IClientFinanceAssetSupportService assetSupportService,
            IAnalysisRequestCompatibilityResolver resolver,
            IAnalysisOrchestrator orchestrator,
            IClientFinanceProjectionService projectionService,
            IConfidenceBreakdownAssembler confidenceBreakdownAssembler,
            IPatternScenarioBranchGenerator branchGenerator)
            : base(serviceProvider)
        {
            _assetSupportService = assetSupportService;
            _resolver = resolver;
            _orchestrator = orchestrator;
            _projectionService = projectionService;
            _confidenceBreakdownAssembler = confidenceBreakdownAssembler;
            _branchGenerator = branchGenerator;
        }

        public async Task<PatternEvaluateResultViewModel> EvaluateAsync(PatternEvaluateRequestViewModel request, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(request);

            var symbol = _assetSupportService.NormalizeSymbol(request.Symbol);
            if (string.IsNullOrWhiteSpace(symbol))
            {
                throw new ArgumentException("Le symbole est obligatoire.", nameof(request));
            }

            var userId = _assetSupportService.GetRequiredCurrentUserId();
            await _assetSupportService.EnsureAssetAsync(symbol, null, ct);

            var runRequest = new AnalysisRunRequestViewModel
            {
                Symbol = symbol,
                RequestedPatternIds = [],
                HoldingContext = request.HoldingContext
            };

            var resolvedRequest = await _resolver.ResolveAsync(runRequest, userId, ct);
            var analysisResponse = await _orchestrator.RunAnalysisAsync(resolvedRequest, ct);

            var analysisRun = await _financeDbContext.AnalysisRuns
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == analysisResponse.AnalysisId && x.UserId == userId, ct);

            var snapshot = analysisRun == null ? null : _projectionService.TryReadSnapshot(analysisRun.RawPayload);

            var persistedAssessments = await _financeDbContext.PatternAssessments
                .AsNoTracking()
                .Where(x => x.AnalysisRunId == analysisResponse.AnalysisId)
                .ToListAsync(ct);

            var candidates = BuildCandidates(snapshot, persistedAssessments);
            var supportResistanceZones = BuildSupportResistanceZones(analysisResponse.Candles);

            return new PatternEvaluateResultViewModel
            {
                AnalysisId = analysisResponse.AnalysisId,
                Symbol = symbol,
                Candidates = candidates,
                SupportResistanceZones = supportResistanceZones
            };
        }

        private static List<SupportResistanceZoneViewModel> BuildSupportResistanceZones(IReadOnlyList<TickerCandle> candles)
        {
            return SupportResistanceDetector.Detect(candles)
                .Select(zone => new SupportResistanceZoneViewModel
                {
                    PriceLow = zone.PriceLow,
                    PriceHigh = zone.PriceHigh,
                    PriceMid = zone.PriceMid,
                    TouchCount = zone.TouchCount,
                    ZoneType = zone.ZoneType,
                    Strength = zone.Strength
                })
                .ToList();
        }

        public async Task<List<PatternCatalogViewModel>> GetPatternCatalogAsync(CancellationToken ct = default)
        {
            return await _financeDbContext.PatternDefinitions
                .AsNoTracking()
                .Select(pattern => new PatternCatalogViewModel
                {
                    Id = pattern.PatternId,
                    Label = pattern.DisplayName,
                    Family = pattern.Family,
                    Description = pattern.Description,
                    Direction = pattern.Direction,
                    FamilyLabel = pattern.FamilyLabel,
                    DirectionLabel = pattern.DirectionLabel,
                    AnalysisNarrative = pattern.AnalysisNarrative,
                    Reliability = pattern.Reliability,
                    ReliabilityLabel = pattern.ReliabilityLabel
                })
                .ToListAsync(ct);
        }

        public async Task<List<AnalysisConceptViewModel>> GetAnalysisConceptsAsync(CancellationToken ct = default)
        {
            return await _financeDbContext.AnalysisConceptExplanations
                .AsNoTracking()
                .Select(concept => new AnalysisConceptViewModel
                {
                    Code = concept.Code,
                    Label = concept.Label,
                    Explanation = concept.Explanation
                })
                .ToListAsync(ct);
        }

        public async Task<PatternDetailViewModel?> GetPatternDetailAsync(string analysisId, string patternId, bool holdsInstrument, CancellationToken ct = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(analysisId);
            ArgumentException.ThrowIfNullOrWhiteSpace(patternId);

            var userId = _assetSupportService.GetRequiredCurrentUserId();

            var analysisRun = await _financeDbContext.AnalysisRuns
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == analysisId && x.UserId == userId, ct);

            if (analysisRun == null)
            {
                return null;
            }

            var snapshot = _projectionService.TryReadSnapshot(analysisRun.RawPayload);
            if (snapshot == null)
            {
                return null;
            }

            var patternRow = snapshot.PatternRows
                .FirstOrDefault(x => string.Equals(x.PatternId, patternId, StringComparison.OrdinalIgnoreCase));

            if (patternRow == null)
            {
                return null;
            }

            var assessment = patternRow.PatternAssessmentPayload;
            var confidenceBreakdown = _confidenceBreakdownAssembler.Build(assessment);
            var confidenceBreakdownVm = _projectionService.BuildConfidenceBreakdownViewModel(confidenceBreakdown);

            var recommendation = snapshot.Recommendation?.RecommendationPayload;
            var posture = _projectionService.BuildRecommendationSummary(recommendation, holdsInstrument, MapStrength(assessment.Scoring.ConfidenceLabel));

            var branches = _branchGenerator.Generate(assessment, holdsInstrument);

            return new PatternDetailViewModel
            {
                PatternId = assessment.PatternId,
                DisplayName = string.IsNullOrWhiteSpace(assessment.DisplayName) ? assessment.PatternId : assessment.DisplayName,
                Phase = assessment.Detection.CurrentPhaseLabel,
                ConfidenceBreakdown = confidenceBreakdownVm,
                NecklinePrice = ExtractNecklinePrice(assessment),
                TargetPrice = ExtractTargetPrice(assessment),
                InvalidationPrice = assessment.Invalidation.InvalidationLevel,
                LifecyclePhaseCode = assessment.Detection.CurrentPhaseCode,
                DetectionStatus = assessment.Detection.Status.ToString(),
                ValidationState = assessment.Validation.State,
                InvalidationState = assessment.Invalidation.State,
                ScenarioBranches = branches,
                Posture = posture
            };
        }

        private static List<PatternCandidateViewModel> BuildCandidates(
            PersistedAnalysisSnapshotPayloadReadModel? snapshot,
            IReadOnlyList<Datas.Entities.PatternAssessment> persistedAssessments)
        {
            if (snapshot == null)
            {
                return [];
            }

            var primaryPatternId = snapshot.PrimaryPattern?.PatternId;

            return snapshot.PatternRows
                .OrderBy(x => x.DisplayRank)
                .Select(row => MapPatternRowToCandidate(
                    row,
                    primaryPatternId,
                    persistedAssessments.FirstOrDefault(x => string.Equals(x.PatternId, row.PatternId, StringComparison.OrdinalIgnoreCase))))
                .ToList();
        }

        private static PatternCandidateViewModel MapPatternRowToCandidate(
            AnalysisSnapshotPatternRow row,
            string? primaryPatternId,
            Datas.Entities.PatternAssessment? persistedAssessment)
        {
            var assessment = row.PatternAssessmentPayload;
            return new PatternCandidateViewModel
            {
                PatternId = row.PatternId,
                DisplayName = string.IsNullOrWhiteSpace(assessment.DisplayName) ? row.PatternId : assessment.DisplayName,
                Confidence = assessment.Scoring.ConfidenceScore,
                Probability = persistedAssessment?.Probability ?? assessment.Scoring.ProbabilityScore ?? assessment.Scoring.ConfidenceScore,
                ConfidenceLabel = assessment.Scoring.ConfidenceLabel,
                Phase = assessment.Detection.CurrentPhaseLabel,
                IsPrimary = string.Equals(row.PatternId, primaryPatternId, StringComparison.OrdinalIgnoreCase),
                NecklinePrice = persistedAssessment?.NecklinePrice ?? ExtractNecklinePrice(assessment),
                TargetPrice = persistedAssessment?.TargetPrice ?? ExtractTargetPrice(assessment),
                InvalidationPrice = persistedAssessment?.InvalidationPrice ?? assessment.Invalidation.InvalidationLevel
            };
        }

        private static decimal? ExtractNecklinePrice(PatternAssessmentContract assessment)
        {
            var necklinePoint = assessment.Detection.StructuralPoints
                .FirstOrDefault(p => p.PointType.Contains("neckline", StringComparison.OrdinalIgnoreCase)
                    || p.PointType.Contains("breakout", StringComparison.OrdinalIgnoreCase));
            return necklinePoint?.Price;
        }

        private static decimal? ExtractTargetPrice(PatternAssessmentContract assessment)
        {
            var targetPoint = assessment.Detection.StructuralPoints
                .FirstOrDefault(p => p.PointType.Contains("target", StringComparison.OrdinalIgnoreCase));
            return targetPoint?.Price;
        }

        private static RecommendationStrengthEnum? MapStrength(string? confidenceLabel)
        {
            return (confidenceLabel ?? string.Empty).Trim().ToUpperInvariant() switch
            {
                "HIGH" => RecommendationStrengthEnum.High,
                "MEDIUM" => RecommendationStrengthEnum.Medium,
                "LOW" => RecommendationStrengthEnum.Low,
                _ => null
            };
        }
    }
}
