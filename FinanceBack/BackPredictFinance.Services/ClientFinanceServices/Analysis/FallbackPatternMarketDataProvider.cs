using System.Text.Json;
using BackPredictFinance.Common;
using BackPredictFinance.Common.MarketData;
using BackPredictFinance.Datas.Context;
using BackPredictFinance.Patterns.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BackPredictFinance.Services.ClientFinanceServices.Analysis
{
    public sealed class FallbackPatternMarketDataProvider : IPatternMarketDataProvider
    {
        private readonly IPatternMarketDataProvider _innerProvider;
        private readonly FinanceDbContext _context;
        private readonly IOptions<MarketDataOptions> _options;
        private readonly IDegradedModeState _degradedModeState;
        private readonly ILogger<FallbackPatternMarketDataProvider> _logger;

        public FallbackPatternMarketDataProvider(
            PatternMarketDataProvider innerProvider,
            FinanceDbContext context,
            IOptions<MarketDataOptions> options,
            IDegradedModeState degradedModeState,
            ILogger<FallbackPatternMarketDataProvider> logger)
            : this((IPatternMarketDataProvider)innerProvider, context, options, degradedModeState, logger)
        {
        }

        internal FallbackPatternMarketDataProvider(
            IPatternMarketDataProvider innerProvider,
            FinanceDbContext context,
            IOptions<MarketDataOptions> options,
            IDegradedModeState degradedModeState,
            ILogger<FallbackPatternMarketDataProvider> logger)
        {
            _innerProvider = innerProvider;
            _context = context;
            _options = options;
            _degradedModeState = degradedModeState;
            _logger = logger;
        }

        /// <summary>
        /// Décore le provider de marché principal (Yahoo) d'un repli sur les dernières bougies
        /// persistées en base si le fournisseur en direct échoue. N'intercepte que les erreurs
        /// transitoires/réseau plausibles (voir liste ci-dessous) : une erreur de programmation ne
        /// doit pas être masquée par un faux positif de "mode dégradé".
        /// </summary>
        public async Task<TickerTimeSeriesResponse> GetTimeSeriesAsync(string symbol, string interval, int outputSize, CancellationToken ct = default)
        {
            try
            {
                return await _innerProvider.GetTimeSeriesAsync(symbol, interval, outputSize, ct);
            }
#pragma warning disable S2139 // Contexte deja loggue (LogWarning avec symbole et type d'exception) avant le rethrow, qui preserve la stack trace d'origine pour ExceptionMiddleware.
            catch (Exception ex) when (ex is HttpRequestException or InvalidOperationException or JsonException or CustomException or TaskCanceledException)
            {
                var fallback = await TryBuildFallbackResponseAsync(symbol, interval, outputSize, ct);
                if (fallback is not null)
                {
                    return fallback;
                }

                _logger.LogWarning(
                    ex,
                    "FallbackPatternMarketDataProvider: fournisseur de marché en échec pour {Symbol} ({ExceptionType}) et aucun snapshot de repli exploitable.",
                    symbol,
                    ex.GetType().Name);

                throw;
            }
#pragma warning restore S2139
        }

        private async Task<TickerTimeSeriesResponse?> TryBuildFallbackResponseAsync(string symbol, string interval, int outputSize, CancellationToken ct)
        {
            var normalizedSymbol = (symbol ?? string.Empty).Trim().ToUpperInvariant();
            var normalizedInterval = string.IsNullOrWhiteSpace(interval) ? "1d" : interval.Trim();

            var asset = await _context.Assets
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Symbol == normalizedSymbol, ct);

            if (asset is null)
            {
                return null;
            }

            var snapshots = await _context.AssetCandleSnapshots
                .AsNoTracking()
                .Where(x => x.AssetId == asset.Id && x.Interval == normalizedInterval)
                .OrderBy(x => x.TimestampUtc)
                .ToListAsync(ct);

            if (snapshots.Count == 0)
            {
                return null;
            }

            // Le repli n'est accepté que si le dernier snapshot connu n'est pas trop vieux
            // (DegradedModeMaxSnapshotAgeHours) : au-delà, une analyse basée dessus induirait
            // l'utilisateur en erreur avec des niveaux de prix obsolètes plutôt que de simplement échouer.
            var latestTimestampUtc = snapshots[^1].TimestampUtc;
            var ageHours = (DateTime.UtcNow - latestTimestampUtc).TotalHours;
            if (ageHours > _options.Value.DegradedModeMaxSnapshotAgeHours)
            {
                _logger.LogWarning(
                    "FallbackPatternMarketDataProvider: snapshot le plus récent pour {Symbol} date de {AgeHours:F1}h, au-delà du seuil de {ThresholdHours}h, repli refusé.",
                    normalizedSymbol,
                    ageHours,
                    _options.Value.DegradedModeMaxSnapshotAgeHours);
                return null;
            }

            _logger.LogWarning(
                "FallbackPatternMarketDataProvider: repli accepté pour {Symbol} sur snapshot du {LatestTimestampUtc:O} ({AgeHours:F1}h).",
                normalizedSymbol,
                latestTimestampUtc,
                ageHours);

            // Signale au reste du pipeline (jusqu'à la réponse client) que l'analyse tourne sur des
            // données de repli et non sur le flux temps réel, pour que l'avertissement soit répercuté
            // à l'utilisateur plutôt que de laisser croire à une analyse à jour.
            _degradedModeState.MarkDegraded(latestTimestampUtc);

            var takeCount = Math.Min(Math.Max(outputSize, 1), snapshots.Count);
            var candles = snapshots
                .TakeLast(takeCount)
                .Select(x => new TickerCandle
                {
                    Date = x.TimestampUtc,
                    Open = x.Open,
                    High = x.High,
                    Low = x.Low,
                    Close = x.Close,
                    Volume = x.Volume
                })
                .ToList();

            return new TickerTimeSeriesResponse
            {
                Symbol = normalizedSymbol,
                Interval = normalizedInterval,
                OutputSize = candles.Count,
                Candles = candles
            };
        }
    }
}
