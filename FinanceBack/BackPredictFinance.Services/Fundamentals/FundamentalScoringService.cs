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

        // Catalogue des metriques entrant dans le score composite, regroupees par categorie (profitability,
        // liquidity, debt, valuation, dividend, growth). Le booleen HigherIsBetter pilote le sens du percentile
        // dans ComputePercentile : true pour les metriques ou une valeur plus elevee est favorable
        // (rentabilite, marge, liquidite, dividende, croissance), false pour celles ou une valeur plus faible
        // est favorable (endettement, ratios de valorisation type PE/PEG/price-to-book).
        private static readonly MetricDefinition[] MetricDefinitions =
        [
            new("returnOnEquity", "profitability", true, static x => x.ReturnOnEquity),
            new("operatingMargin", "profitability", true, static x => x.OperatingMargin),
            new("currentRatio", "liquidity", true, static x => x.CurrentRatio),
            new("debtToEquity", "debt", false, static x => x.DebtToEquity),
            new("trailingPe", "valuation", false, static x => x.TrailingPe),
            new("pegRatio", "valuation", false, static x => x.PegRatio),
            new("priceToBook", "valuation", false, static x => x.PriceToBook),
            new("dividendYield", "dividend", true, static x => x.DividendYield),
            new("revenueGrowth", "growth", true, static x => x.RevenueGrowth),
            new("earningsGrowth", "growth", true, static x => x.EarningsGrowth)
        ];

        private readonly IFundamentalsProvider _fundamentalsProvider;
        private readonly ILogger<FundamentalScoringService> _scoringLogger;
        private readonly IFundamentalScoringPolicyService _fundamentalScoringPolicyService;

        public FundamentalScoringService(
            IServiceProvider serviceProvider,
            IFundamentalsProvider fundamentalsProvider,
            ILogger<FundamentalScoringService> scoringLogger,
            IFundamentalScoringPolicyService fundamentalScoringPolicyService)
            : base(serviceProvider)
        {
            _fundamentalsProvider = fundamentalsProvider;
            _scoringLogger = scoringLogger;
            _fundamentalScoringPolicyService = fundamentalScoringPolicyService;
        }

        /// <summary>
        /// Calcule le score composite fondamental pour les symboles demandes, en deux temps :
        /// 1) construit d'abord le scoring percentile de tout l'univers PEA confirme eligible (necessaire
        ///    comme base de comparaison, meme pour des symboles non demandes) ;
        /// 2) resout ensuite chaque symbole demande depuis ce cache d'univers si possible, ou via un appel
        ///    fondamentaux ad hoc sinon (le score reste alors non utilisable pour le rang/percentile puisqu'il
        ///    n'a pas ete inclus dans la distribution de reference).
        /// </summary>
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
                throw new ArgumentException("At least one symbol is required.", nameof(request));
            }

            var activePolicy = await _fundamentalScoringPolicyService.ResolveActiveAsync(ct);

            // Nombre minimal de categories (sur 6) devant disposer d'un score pour qu'un TotalScore soit
            // considere utilisable ; borne au plancher/plafond de la politique active (ou des valeurs par
            // defaut si aucune politique n'est active en base) pour empecher un appelant de demander un
            // score "utilisable" avec une couverture de donnees trop faible pour etre significative.
            var minCategoriesRequired = Math.Clamp(
                request.MinCategoriesRequired,
                activePolicy.MinimumCategoriesRequiredFloor,
                activePolicy.MinimumCategoriesRequiredCeiling);
            var minimumSectorSampleSize = activePolicy.MinimumSectorSampleSize;
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
            var universeMetricValuesBySector = BuildUniverseMetricValuesBySector(universeSnapshots);
            var universeScored = BuildUniverseScores(universeEntries, universeSnapshots, universeMetricValues, universeMetricValuesBySector, minCategoriesRequired, activePolicy.CoveragePenaltySupported, minimumSectorSampleSize);
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
                    universeMetricValuesBySector,
                    minCategoriesRequired,
                    activePolicy.CoveragePenaltySupported,
                    allowUsableScore: false,
                    universeSize: rankableUniverseSize,
                    rankPosition: null,
                    additionalNotes: rankableUniverseSize == 0 ? [.. notes, "No confirmed eligible PEA universe member has enough live data for percentile scoring."] : notes,
                    minimumSectorSampleSize: minimumSectorSampleSize);

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

        // Recupere les fondamentaux de chaque membre de l'univers ; un symbole en echec (fondamentaux
        // indisponibles) est simplement absent du dictionnaire retourne plutot que de faire echouer tout
        // le scoring de l'univers.
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

        // Construit, pour chaque metrique, la distribution des valeurs disponibles sur tout l'univers.
        // Cette distribution globale sert de base de comparaison par defaut pour le percentile, et de
        // fallback quand l'echantillon sectoriel est trop petit (voir MinimumSectorSampleSize).
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

        // Meme construction que BuildUniverseMetricValues, mais regroupee par secteur : un titre est compare
        // en priorite a ses pairs sectoriels plutot qu'a l'univers entier, car un score absolu (ex. PE brut)
        // n'a de sens que relativement a un secteur (les normes de marge, d'endettement ou de valorisation
        // varient fortement d'un secteur a l'autre).
        private static Dictionary<string, Dictionary<string, List<decimal>>> BuildUniverseMetricValuesBySector(
            Dictionary<string, MarketFundamentalData> universeSnapshots)
        {
            var metricValuesBySector = new Dictionary<string, Dictionary<string, List<decimal>>>(StringComparer.OrdinalIgnoreCase);
            foreach (var sectorGroup in universeSnapshots.Values.GroupBy(x => x.Sector ?? string.Empty))
            {
                if (sectorGroup.Key.Length == 0)
                {
                    // Secteur inconnu/non renseigne : impossible de constituer un groupe de comparaison
                    // sectoriel coherent, on laisse ces titres retomber sur le fallback univers global.
                    continue;
                }

                var metricValues = new Dictionary<string, List<decimal>>(StringComparer.OrdinalIgnoreCase);
                foreach (var metric in MetricDefinitions)
                {
                    metricValues[metric.Code] = sectorGroup
                        .Select(metric.Read)
                        .Where(static x => x.HasValue)
                        .Select(static x => x!.Value)
                        .ToList();
                }

                metricValuesBySector[sectorGroup.Key] = metricValues;
            }

            return metricValuesBySector;
        }

        private static List<FundamentalScoreResult> BuildUniverseScores(
            List<AssetPeaEligibility> universeEntries,
            Dictionary<string, MarketFundamentalData> universeSnapshots,
            Dictionary<string, List<decimal>> universeMetricValues,
            Dictionary<string, Dictionary<string, List<decimal>>> universeMetricValuesBySector,
            int minCategoriesRequired,
            bool coveragePenaltyEnabled,
            int minimumSectorSampleSize)
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
                    universeMetricValuesBySector,
                    minCategoriesRequired,
                    coveragePenaltyEnabled,
                    allowUsableScore: true,
                    universeSize: universeEntries.Count,
                    rankPosition: null,
                    additionalNotes: [],
                    minimumSectorSampleSize: minimumSectorSampleSize));
            }

            return results;
        }

        // Seuls les scores utilisables (donnees suffisantes + eligibilite confirmee) participent au
        // classement ; les ex-aequo sur TotalScore sont departages par symbole pour un rang deterministe
        // et stable d'un appel a l'autre.
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
            Dictionary<string, Dictionary<string, List<decimal>>> universeMetricValuesBySector,
            int minCategoriesRequired,
            bool coveragePenaltyEnabled,
            bool allowUsableScore,
            int universeSize,
            int? rankPosition,
            List<string> additionalNotes,
            int minimumSectorSampleSize)
        {
            var missingMetrics = new List<string>();
            var fallbackMetrics = new List<string>();
            var notes = new List<string>(additionalNotes);

            decimal? profitabilityScore = ComputeCategoryScore("profitability", fundamentals, universeMetricValues, universeMetricValuesBySector, missingMetrics, fallbackMetrics, minimumSectorSampleSize);
            decimal? liquidityScore = ComputeCategoryScore("liquidity", fundamentals, universeMetricValues, universeMetricValuesBySector, missingMetrics, fallbackMetrics, minimumSectorSampleSize);
            decimal? debtScore = ComputeCategoryScore("debt", fundamentals, universeMetricValues, universeMetricValuesBySector, missingMetrics, fallbackMetrics, minimumSectorSampleSize);
            decimal? valuationScore = ComputeCategoryScore("valuation", fundamentals, universeMetricValues, universeMetricValuesBySector, missingMetrics, fallbackMetrics, minimumSectorSampleSize);
            decimal? dividendScore = ComputeCategoryScore("dividend", fundamentals, universeMetricValues, universeMetricValuesBySector, missingMetrics, fallbackMetrics, minimumSectorSampleSize);
            decimal? growthScore = ComputeCategoryScore("growth", fundamentals, universeMetricValues, universeMetricValuesBySector, missingMetrics, fallbackMetrics, minimumSectorSampleSize);

            var categoryScores = new decimal?[]
            {
                profitabilityScore,
                liquidityScore,
                debtScore,
                valuationScore,
                dividendScore,
                growthScore
            };

            var categoriesPresent = categoryScores.Count(x => x.HasValue);
            // Coverage = proportion des 6 categories effectivement scorees (donnees disponibles). Sert a la
            // fois de metrique de transparence et, si coveragePenaltyEnabled, de facteur multiplicatif qui
            // penalise un score base sur peu de categories (un titre note uniquement sur "dividend" ne doit
            // pas rivaliser a egalite avec un titre note sur les 6 categories).
            var coverage = decimal.Round(categoriesPresent / (decimal)categoryScores.Length, 4);
            decimal? totalScore = null;
            var usableScore = false;

            if (allowUsableScore && peaEligibility.Status == PeaEligibilityStatusEnum.ConfirmedEligible && categoriesPresent >= minCategoriesRequired)
            {
                // Score total = moyenne des percentiles de categorie disponibles, optionnellement ponderee
                // par la couverture de donnees (average * coverage) pour desavantager les scores partiels.
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

            var percentileGroupLabel = fundamentals?.Sector ?? string.Empty;
            var usedGlobalUniverseFallback = fallbackMetrics.Count > 0;
            if (usedGlobalUniverseFallback)
            {
                notes.Add(percentileGroupLabel.Length > 0
                    ? "Percentile calculated on global universe: insufficient sector sample"
                    : "Percentile calculated on global universe: unknown sector");
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
                GrowthScore = growthScore,
                MissingMetrics = missingMetrics,
                RankPosition = usableScore ? rankPosition : null,
                UniverseSize = universeSize,
                Notes = notes,
                PeaEligibility = peaEligibility,
                PercentileGroupLabel = percentileGroupLabel,
                UsedGlobalUniverseFallback = usedGlobalUniverseFallback
            };
        }

        // Score d'une categorie (ex. "profitability") = moyenne des percentiles de ses metriques disponibles.
        // Une metrique manquante ou sans groupe de comparaison exploitable est ecartee (et tracee dans
        // missingMetrics) sans faire echouer la categorie tant qu'au moins une metrique est utilisable.
        private static decimal? ComputeCategoryScore(
            string categoryCode,
            MarketFundamentalData? fundamentals,
            Dictionary<string, List<decimal>> universeMetricValues,
            Dictionary<string, Dictionary<string, List<decimal>>> universeMetricValuesBySector,
            List<string> missingMetrics,
            List<string> fallbackMetrics,
            int minimumSectorSampleSize)
        {
            if (fundamentals == null)
            {
                missingMetrics.AddRange(MetricDefinitions.Where(x => x.CategoryCode == categoryCode).Select(x => x.Code));
                return null;
            }

            var sector = fundamentals.Sector ?? string.Empty;
            var sectorMetricValues = sector.Length > 0 && universeMetricValuesBySector.TryGetValue(sector, out var sectorValues)
                ? sectorValues
                : null;

            var metricScores = new List<decimal>();
            foreach (var metric in MetricDefinitions.Where(x => x.CategoryCode == categoryCode))
            {
                var value = metric.Read(fundamentals);
                if (!value.HasValue)
                {
                    missingMetrics.Add(metric.Code);
                    continue;
                }

                var comparisonSet = ResolveComparisonSet(metric.Code, sectorMetricValues, universeMetricValues, fallbackMetrics, minimumSectorSampleSize);
                if (comparisonSet is null || comparisonSet.Count == 0)
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

        // Choisit le groupe de comparaison pour le percentile d'une metrique : priorite au secteur si son
        // echantillon atteint minimumSectorSampleSize (percentile sectoriel plus pertinent qu'un percentile
        // absolu, cf. BuildUniverseMetricValuesBySector), sinon repli sur l'univers global (et la metrique est
        // ajoutee a fallbackMetrics pour que l'appelant puisse tracer ce repli dans les Notes du resultat).
        // minimumSectorSampleSize provient de la politique de scoring active (voir IFundamentalScoringPolicyService).
        private static List<decimal>? ResolveComparisonSet(
            string metricCode,
            Dictionary<string, List<decimal>>? sectorMetricValues,
            Dictionary<string, List<decimal>> universeMetricValues,
            List<string> fallbackMetrics,
            int minimumSectorSampleSize)
        {
            if (sectorMetricValues is not null
                && sectorMetricValues.TryGetValue(metricCode, out var sectorList)
                && sectorList.Count >= minimumSectorSampleSize)
            {
                return sectorList;
            }

            fallbackMetrics.Add(metricCode);
            return universeMetricValues.TryGetValue(metricCode, out var globalList) ? globalList : null;
        }

        // Percentile rank classique (rang fractionnaire) : proportion de l'echantillon strictement inferieure
        // a la valeur, plus la moitie des valeurs egales (methode standard de gestion des ex-aequo pour eviter
        // qu'un groupe de valeurs identiques biaise le rang). Avec un echantillon d'une seule valeur, le
        // percentile n'a pas de sens statistique : on retourne 1 (traite comme le meilleur du groupe) par
        // convention plutot que de propager une division degenerescente.
        // Si higherIsBetter est faux (ex. endettement, PE, PEG : une valeur plus basse est preferable), le
        // percentile est inverse (1 - percentile) pour que "bon score" signifie toujours "proche de 1".
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
