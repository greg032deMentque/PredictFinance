using BackPredictFinance.Common.Fundamentals;
using BackPredictFinance.Common.MarketData;
using BackPredictFinance.Common.enums;
using BackPredictFinance.Datas.Entities;
using BackPredictFinance.Services.TwelveDataServices;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BackPredictFinance.Services.Fundamentals
{
    /// <summary>
    /// Expose le scoring fondamental backend-owned d'un univers donné.
    /// </summary>
    public interface IFundamentalScoringService
    {
        /// <summary>
        /// Calcule le scoring fondamental pour la requête fournie.
        /// </summary>
        Task<FundamentalScoreResponse> ScoreAsync(FundamentalScoreRequest request, CancellationToken ct = default);
    }

    /// <summary>
    /// Implémente le scoring fondamental et la projection des rangs de l'univers.
    /// </summary>
    public sealed class FundamentalScoringService : BaseService, IFundamentalScoringService
    {
        private const string SupportedUniverseId = FundamentalScoringPolicyDefaults.SupportedUniverseId;
        private const string ScoringVersion = FundamentalScoringPolicyDefaults.ScoringVersion;
        private const string EligibilityPolicyVersion = FundamentalScoringPolicyDefaults.EligibilityPolicyVersion;
        private const string ProviderId = FundamentalScoringPolicyDefaults.ProviderId;
        private const string AsOfUtcSemantics = FundamentalScoringPolicyDefaults.AsOfUtcSemantics;

        private static readonly MetricDefinition[] MetricDefinitions =
        [
            new("returnOnEquity", "profitability", true, static x => x.ReturnOnEquity),
            new("operatingMargin", "profitability", true, static x => x.OperatingMargin),
            new("currentRatio", "liquidity", true, static x => x.CurrentRatio),
            new("debtToEquity", "debt", false, static x => x.DebtToEquity),
            new("trailingPe", "valuation", false, static x => x.TrailingPe),
            new("dividendYield", "dividend", true, static x => x.DividendYield)
        ];

        private readonly IFundamentalsProvider _fundamentalsProvider;
        private readonly ILogger<FundamentalScoringService> _scoringLogger;

        public FundamentalScoringService(
            IServiceProvider serviceProvider,
            IFundamentalsProvider fundamentalsProvider,
            ILogger<FundamentalScoringService> scoringLogger)
            : base(serviceProvider)
        {
            _fundamentalsProvider = fundamentalsProvider;
            _scoringLogger = scoringLogger;
        }

        public async Task<FundamentalScoreResponse> ScoreAsync(FundamentalScoreRequest request, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(request);

            var normalizedUniverseId = NormalizeUniverseId(request.UniverseId);
            if (!string.Equals(normalizedUniverseId, SupportedUniverseId, StringComparison.Ordinal))
            {
                throw new InvalidOperationException($"Unsupported universe '{request.UniverseId}'. Only {SupportedUniverseId} is supported in V1.");
            }

            var requestedSymbols = (request.Symbols ?? [])
                .Select(NormalizeSymbol)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            if (requestedSymbols.Count == 0)
            {
                throw new ArgumentException("At least one symbol is required.", nameof(request.Symbols));
            }

            var minCategoriesRequired = Math.Clamp(request.MinCategoriesRequired, 1, 5);
            var runAsOfUtc = DateTime.UtcNow;

            var universeEntries = await _financeDbContext.Set<AssetPeaEligibility>()
                .AsNoTracking()
                .Include(x => x.Asset)
                .Where(x => x.UniverseId == normalizedUniverseId && x.EligibilityStatus == PeaEligibilityStatusEnum.ConfirmedEligible)
                .Where(x => x.Asset.AssetType == AssetTypeEnum.Stock)
                .OrderBy(x => x.Asset.Symbol)
                .ToListAsync(ct);

            var universeSnapshots = await LoadUniverseSnapshotsAsync(universeEntries, ct);
            var universeMetricValues = BuildUniverseMetricValues(universeSnapshots);
            var universeScored = BuildUniverseScores(universeEntries, universeSnapshots, universeMetricValues, minCategoriesRequired, request.CoveragePenaltyEnabled);
            var universeRanks = BuildUniverseRanks(universeScored);
            var rankableUniverseSize = universeRanks.Count;
            foreach (var scored in universeScored)
            {
                scored.UniverseSize = rankableUniverseSize;
                scored.RankPosition = request.IncludeRankPosition && universeRanks.TryGetValue(scored.Symbol, out var rank) ? rank : null;
                if (rankableUniverseSize == 0)
                {
                    scored.Notes.Add("No confirmed eligible PEA universe member has enough live data for percentile scoring.");
                }
            }

            var universeResults = universeScored.ToDictionary(x => x.Symbol, StringComparer.OrdinalIgnoreCase);

            var registryEntries = await _financeDbContext.Set<AssetPeaEligibility>()
                .AsNoTracking()
                .Include(x => x.Asset)
                .Where(x => x.UniverseId == normalizedUniverseId)
                .Where(x => requestedSymbols.Contains(x.Asset.Symbol))
                .ToListAsync(ct);
            var registryBySymbol = registryEntries.ToDictionary(x => x.Asset.Symbol, StringComparer.OrdinalIgnoreCase);

            var results = new List<FundamentalScoreResult>(requestedSymbols.Count);
            foreach (var requestedSymbol in requestedSymbols)
            {
                if (universeResults.TryGetValue(requestedSymbol, out var inUniverseResult))
                {
                    if (!request.IncludeRankPosition)
                    {
                        inUniverseResult.RankPosition = null;
                    }

                    results.Add(inUniverseResult);
                    continue;
                }

                registryBySymbol.TryGetValue(requestedSymbol, out var registryEntry);
                var peaEligibility = MapEligibility(registryEntry);
                var notes = new List<string>();
                MarketFundamentalData? fundamentals = null;
                try
                {
                    fundamentals = await _fundamentalsProvider.GetFundamentalsAsync(requestedSymbol, ct);
                }
                catch (Exception ex) when (ex is InvalidOperationException or HttpRequestException or System.Text.Json.JsonException)
                {
                    _scoringLogger.LogWarning(ex, "FundamentalScoringService.ScoreAsync: fondamentaux indisponibles pour {Symbol} ({ExceptionType})", requestedSymbol, ex.GetType().Name);
                    notes.Add($"Fundamental data unavailable for {requestedSymbol}.");
                }

                var result = BuildScoreResult(
                    requestedSymbol,
                    fundamentals?.CompanyName ?? registryEntry?.Asset.Name ?? registryEntry?.Asset.Symbol ?? requestedSymbol,
                    peaEligibility,
                    fundamentals,
                    universeMetricValues,
                    minCategoriesRequired,
                    request.CoveragePenaltyEnabled,
                    allowUsableScore: false,
                    universeSize: rankableUniverseSize,
                    rankPosition: null,
                    additionalNotes: rankableUniverseSize == 0 ? [.. notes, "No confirmed eligible PEA universe member has enough live data for percentile scoring."] : notes);

                if (peaEligibility.Status != PeaEligibilityStatusEnum.ConfirmedEligible)
                {
                    result.Notes.Add("Instrument is not part of the confirmed eligible PEA universe.");
                }

                results.Add(result);
            }

            return new FundamentalScoreResponse
            {
                UniverseId = normalizedUniverseId,
                ScoringVersion = ScoringVersion,
                EligibilityPolicyVersion = EligibilityPolicyVersion,
                ProviderId = ProviderId,
                AsOfUtc = runAsOfUtc,
                AsOfUtcSemantics = AsOfUtcSemantics,
                Results = results
            };
        }

        private async Task<Dictionary<string, MarketFundamentalData>> LoadUniverseSnapshotsAsync(List<AssetPeaEligibility> universeEntries, CancellationToken ct)
        {
            var snapshots = new Dictionary<string, MarketFundamentalData>(StringComparer.OrdinalIgnoreCase);
            foreach (var entry in universeEntries)
            {
                try
                {
                    var snapshot = await _fundamentalsProvider.GetFundamentalsAsync(entry.Asset.Symbol, ct);
                    snapshots[entry.Asset.Symbol] = snapshot;
                }
                catch (Exception ex) when (ex is InvalidOperationException or HttpRequestException or System.Text.Json.JsonException)
                {
                    _scoringLogger.LogWarning(ex, "FundamentalScoringService.LoadUniverseSnapshotsAsync: snapshot indisponible pour {Symbol} ({ExceptionType})", entry.Asset.Symbol, ex.GetType().Name);
                    continue;
                }
            }

            return snapshots;
        }

        private static Dictionary<string, List<decimal>> BuildUniverseMetricValues(Dictionary<string, MarketFundamentalData> universeSnapshots)
        {
            var metricValues = new Dictionary<string, List<decimal>>(StringComparer.OrdinalIgnoreCase);
            foreach (var metric in MetricDefinitions)
            {
                metricValues[metric.Code] = universeSnapshots.Values
                    .Select(metric.Read)
                    .Where(static x => x.HasValue)
                    .Select(static x => x!.Value)
                    .ToList();
            }

            return metricValues;
        }

        private static List<FundamentalScoreResult> BuildUniverseScores(
            List<AssetPeaEligibility> universeEntries,
            Dictionary<string, MarketFundamentalData> universeSnapshots,
            Dictionary<string, List<decimal>> universeMetricValues,
            int minCategoriesRequired,
            bool coveragePenaltyEnabled)
        {
            var results = new List<FundamentalScoreResult>();
            foreach (var entry in universeEntries)
            {
                if (!universeSnapshots.TryGetValue(entry.Asset.Symbol, out var snapshot))
                {
                    results.Add(new FundamentalScoreResult
                    {
                        Symbol = entry.Asset.Symbol,
                        DisplayName = entry.Asset.Name ?? entry.Asset.Symbol,
                        UsableScore = false,
                        CategoriesPresent = 0,
                        CategoryCoverage = 0m,
                        UniverseSize = universeEntries.Count,
                        PeaEligibility = MapEligibility(entry),
                        MissingMetrics = MetricDefinitions.Select(static x => x.Code).ToList(),
                        Notes = ["Fundamental data unavailable for this universe member."]
                    });
                    continue;
                }

                results.Add(BuildScoreResult(
                    entry.Asset.Symbol,
                    snapshot.CompanyName,
                    MapEligibility(entry),
                    snapshot,
                    universeMetricValues,
                    minCategoriesRequired,
                    coveragePenaltyEnabled,
                    allowUsableScore: true,
                    universeSize: universeEntries.Count,
                    rankPosition: null,
                    additionalNotes: []));
            }

            return results;
        }

        private static Dictionary<string, int> BuildUniverseRanks(List<FundamentalScoreResult> results)
        {
            return results
                .Where(x => x.UsableScore && x.TotalScore.HasValue)
                .OrderByDescending(x => x.TotalScore)
                .ThenBy(x => x.Symbol, StringComparer.OrdinalIgnoreCase)
                .Select((x, index) => new { x.Symbol, Rank = index + 1 })
                .ToDictionary(x => x.Symbol, x => x.Rank, StringComparer.OrdinalIgnoreCase);
        }

        private static FundamentalScoreResult BuildScoreResult(
            string symbol,
            string displayName,
            PeaEligibilityInfo peaEligibility,
            MarketFundamentalData? fundamentals,
            Dictionary<string, List<decimal>> universeMetricValues,
            int minCategoriesRequired,
            bool coveragePenaltyEnabled,
            bool allowUsableScore,
            int universeSize,
            int? rankPosition,
            List<string> additionalNotes)
        {
            var missingMetrics = new List<string>();
            var notes = new List<string>(additionalNotes);

            decimal? profitabilityScore = ComputeCategoryScore("profitability", fundamentals, universeMetricValues, missingMetrics);
            decimal? liquidityScore = ComputeCategoryScore("liquidity", fundamentals, universeMetricValues, missingMetrics);
            decimal? debtScore = ComputeCategoryScore("debt", fundamentals, universeMetricValues, missingMetrics);
            decimal? valuationScore = ComputeCategoryScore("valuation", fundamentals, universeMetricValues, missingMetrics);
            decimal? dividendScore = ComputeCategoryScore("dividend", fundamentals, universeMetricValues, missingMetrics);

            var categoryScores = new decimal?[]
            {
                profitabilityScore,
                liquidityScore,
                debtScore,
                valuationScore,
                dividendScore
            };

            var categoriesPresent = categoryScores.Count(x => x.HasValue);
            var coverage = decimal.Round(categoriesPresent / 5m, 4);
            decimal? totalScore = null;
            var usableScore = false;

            if (allowUsableScore && peaEligibility.Status == PeaEligibilityStatusEnum.ConfirmedEligible && categoriesPresent >= minCategoriesRequired)
            {
                var average = categoryScores.Where(x => x.HasValue).Average(x => x!.Value);
                totalScore = coveragePenaltyEnabled
                    ? decimal.Round(average * coverage, 6)
                    : decimal.Round(average, 6);
                usableScore = true;
            }
            else
            {
                if (peaEligibility.Status != PeaEligibilityStatusEnum.ConfirmedEligible)
                {
                    notes.Add("Total score is unavailable because the instrument is not confirmed eligible for the active universe.");
                }
                else if (categoriesPresent < minCategoriesRequired)
                {
                    notes.Add($"Total score is unavailable because only {categoriesPresent} categories are available and the minimum required is {minCategoriesRequired}.");
                }
            }

            return new FundamentalScoreResult
            {
                Symbol = symbol,
                DisplayName = displayName,
                UsableScore = usableScore,
                TotalScore = totalScore,
                CategoriesPresent = categoriesPresent,
                CategoryCoverage = coverage,
                ProfitabilityScore = profitabilityScore,
                LiquidityScore = liquidityScore,
                DebtScore = debtScore,
                ValuationScore = valuationScore,
                DividendScore = dividendScore,
                MissingMetrics = missingMetrics,
                RankPosition = usableScore ? rankPosition : null,
                UniverseSize = universeSize,
                Notes = notes,
                PeaEligibility = peaEligibility
            };
        }

        private static decimal? ComputeCategoryScore(
            string categoryCode,
            MarketFundamentalData? fundamentals,
            Dictionary<string, List<decimal>> universeMetricValues,
            List<string> missingMetrics)
        {
            if (fundamentals == null)
            {
                missingMetrics.AddRange(MetricDefinitions.Where(x => x.CategoryCode == categoryCode).Select(x => x.Code));
                return null;
            }

            var metricScores = new List<decimal>();
            foreach (var metric in MetricDefinitions.Where(x => x.CategoryCode == categoryCode))
            {
                var value = metric.Read(fundamentals);
                if (!value.HasValue)
                {
                    missingMetrics.Add(metric.Code);
                    continue;
                }

                if (!universeMetricValues.TryGetValue(metric.Code, out var comparisonSet) || comparisonSet.Count == 0)
                {
                    missingMetrics.Add(metric.Code);
                    continue;
                }

                metricScores.Add(ComputePercentile(value.Value, comparisonSet, metric.HigherIsBetter));
            }

            if (metricScores.Count == 0)
            {
                return null;
            }

            return decimal.Round(metricScores.Average(), 6);
        }

        private static decimal ComputePercentile(decimal value, List<decimal> comparisonSet, bool higherIsBetter)
        {
            var lessCount = comparisonSet.Count(x => x < value);
            var equalCount = comparisonSet.Count(x => x == value);
            decimal percentile = comparisonSet.Count == 1
                ? 1m
                : (lessCount + ((decimal)equalCount / 2m)) / comparisonSet.Count;

            if (higherIsBetter)
            {
                return decimal.Round(percentile, 6);
            }

            return decimal.Round(1m - percentile, 6);
        }

        private static PeaEligibilityInfo MapEligibility(AssetPeaEligibility? entry)
        {
            if (entry == null)
            {
                return new PeaEligibilityInfo
                {
                    Status = PeaEligibilityStatusEnum.Unknown,
                    SourceType = PeaEligibilitySourceTypeEnum.Unknown,
                    PolicyVersion = EligibilityPolicyVersion
                };
            }

            return new PeaEligibilityInfo
            {
                Status = entry.EligibilityStatus,
                SourceType = entry.SourceType,
                SourceReference = entry.SourceReference,
                CheckedUtc = entry.CheckedUtc,
                PolicyVersion = string.IsNullOrWhiteSpace(entry.PolicyVersion) ? EligibilityPolicyVersion : entry.PolicyVersion,
                ReviewerNote = entry.ReviewerNote
            };
        }

        private static string NormalizeUniverseId(string universeId)
        {
            var normalized = (universeId ?? string.Empty).Trim().ToUpperInvariant();
            if (string.IsNullOrWhiteSpace(normalized))
            {
                throw new ArgumentException("UniverseId is required.", nameof(universeId));
            }

            return normalized;
        }

        private static string NormalizeSymbol(string symbol)
        {
            var normalized = (symbol ?? string.Empty).Trim().ToUpperInvariant();
            if (string.IsNullOrWhiteSpace(normalized))
            {
                throw new ArgumentException("Symbol is required.", nameof(symbol));
            }

            return normalized;
        }

        private sealed record MetricDefinition(string Code, string CategoryCode, bool HigherIsBetter, Func<MarketFundamentalData, decimal?> Read);
    }
}
