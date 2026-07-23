using System.Text.Json;
using BackPredictFinance.Common.enums;
using BackPredictFinance.Common.Fundamentals;
using BackPredictFinance.Datas.Entities;
using BackPredictFinance.Services.Fundamentals;
using BackPredictFinance.ViewModels.ClientFinanceViewModels.Screener;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;

namespace BackPredictFinance.Services.ClientFinanceServices.Screener
{
    public interface IScreenerService
    {
        Task<PagedScreenerViewModel> GetPagedAsync(ScreenerQueryViewModel query, CancellationToken ct = default);
        Task<ScreenerMetaViewModel> GetMetaAsync(CancellationToken ct = default);

        /// <summary>
        /// Exporte les instruments correspondant aux filtres (hors pagination) au format CSV, plafonné à 5000 lignes.
        /// </summary>
        Task<byte[]> ExportCsvAsync(ScreenerQueryViewModel query, CancellationToken ct = default);

        Task<List<ScreenerPresetViewModel>> GetPresetsAsync(CancellationToken ct = default);
        Task<ScreenerPresetViewModel> SavePresetAsync(ScreenerPresetCreateViewModel model, CancellationToken ct = default);
        Task DeletePresetAsync(string presetId, CancellationToken ct = default);
    }

    public sealed class ScreenerService : BaseService, IScreenerService
    {
        private const int ExportMaxRows = 5000;
        private const string MetaCacheKey = "screener:meta";
        private static readonly TimeSpan MetaCacheTtl = TimeSpan.FromHours(1);

        private static readonly HashSet<string> SortWhitelist = new(StringComparer.OrdinalIgnoreCase)
        {
            "Symbol", "Name", "Exchange", "Sector", "Country", "AssetType", "LastPrice", "DayVariationPct",
            "TrailingPE", "DividendYield", "MarketCap"
        };

        private readonly IMemoryCache _memoryCache;

        public ScreenerService(IServiceProvider serviceProvider)
            : base(serviceProvider)
        {
            _memoryCache = serviceProvider.GetRequiredService<IMemoryCache>();
        }

        public async Task<PagedScreenerViewModel> GetPagedAsync(ScreenerQueryViewModel query, CancellationToken ct = default)
        {
            var page = Math.Max(1, query.Page);
            var pageSize = Math.Clamp(query.PageSize, 1, 100);

            var filtered = await BuildFilteredQueryAsync(query, ct);
            var total = await filtered.CountAsync(ct);

            var projected = ApplySort(ProjectWithQuoteAndPea(filtered), query.SortBy, query.SortDirection);

            var items = await projected
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(ct);

            return new PagedScreenerViewModel
            {
                Items = items,
                Total = total,
                Page = page,
                PageSize = pageSize
            };
        }

        public async Task<ScreenerMetaViewModel> GetMetaAsync(CancellationToken ct = default)
        {
            if (_memoryCache.TryGetValue(MetaCacheKey, out ScreenerMetaViewModel? cached) && cached is not null)
            {
                return cached;
            }

            var sectors = await _financeDbContext.Assets
                .AsNoTracking()
                .Where(a => a.Sector != null)
                .Select(a => a.Sector!)
                .Distinct()
                .OrderBy(s => s)
                .ToListAsync(ct);

            var countries = await _financeDbContext.Assets
                .AsNoTracking()
                .Where(a => a.Country != null)
                .Select(a => a.Country!)
                .Distinct()
                .OrderBy(c => c)
                .ToListAsync(ct);

            var meta = new ScreenerMetaViewModel
            {
                Sectors = sectors,
                Countries = countries
            };

            _memoryCache.Set(MetaCacheKey, meta, MetaCacheTtl);

            return meta;
        }

        public async Task<byte[]> ExportCsvAsync(ScreenerQueryViewModel query, CancellationToken ct = default)
        {
            var filtered = await BuildFilteredQueryAsync(query, ct);
            var projected = ApplySort(ProjectWithQuoteAndPea(filtered), query.SortBy, query.SortDirection);

            var items = await projected
                .Take(ExportMaxRows)
                .ToListAsync(ct);

            return ScreenerCsvWriter.BuildCsv(items);
        }

        private async Task<IQueryable<Asset>> BuildFilteredQueryAsync(ScreenerQueryViewModel query, CancellationToken ct)
        {
            var baseQuery = _financeDbContext.Assets
                .AsNoTracking();

            if (query.Sectors is { Count: > 0 })
                baseQuery = baseQuery.Where(a => a.Sector != null && query.Sectors.Contains(a.Sector));

            if (query.Countries is { Count: > 0 })
                baseQuery = baseQuery.Where(a => a.Country != null && query.Countries.Contains(a.Country));

            if (query.AssetType.HasValue)
                baseQuery = baseQuery.Where(a => (int)a.AssetType == query.AssetType.Value);

            if (query.PeaOnly)
            {
                var peaEligibleIds = _financeDbContext.AssetPeaEligibilities
                    .Where(p => p.EligibilityStatus == PeaEligibilityStatusEnum.ConfirmedEligible)
                    .Select(p => p.AssetId);

                baseQuery = baseQuery.Where(a => peaEligibleIds.Contains(a.Id));
            }

            var search = query.Search?.Trim();
            if (!string.IsNullOrEmpty(search))
            {
                baseQuery = baseQuery.Where(a =>
                    EF.Functions.Like(a.Symbol, $"%{search}%")
                    || EF.Functions.Like(a.Name ?? string.Empty, $"%{search}%"));
            }

            if (query.MinPE.HasValue || query.MaxPE.HasValue || query.MinDividendYield.HasValue || query.MinMarketCap.HasValue)
            {
                var latestFundamentals = GetLatestByAssetId(_financeDbContext.AssetFundamentalsSnapshots);

                if (query.MinPE.HasValue)
                    latestFundamentals = latestFundamentals.Where(f => f.TrailingPE.HasValue && f.TrailingPE.Value >= query.MinPE.Value);

                if (query.MaxPE.HasValue)
                    latestFundamentals = latestFundamentals.Where(f => f.TrailingPE.HasValue && f.TrailingPE.Value <= query.MaxPE.Value);

                if (query.MinDividendYield.HasValue)
                    latestFundamentals = latestFundamentals.Where(f => f.DividendYield.HasValue && f.DividendYield.Value >= query.MinDividendYield.Value);

                if (query.MinMarketCap.HasValue)
                    latestFundamentals = latestFundamentals.Where(f => f.MarketCap.HasValue && f.MarketCap.Value >= query.MinMarketCap.Value);

                var matchingAssetIds = latestFundamentals.Select(f => f.AssetId);
                baseQuery = baseQuery.Where(a => matchingAssetIds.Contains(a.Id));
            }

            if (query.MinScore.HasValue)
            {
                var candidateSymbols = await baseQuery.Select(a => a.Symbol).ToListAsync(ct);
                if (candidateSymbols.Count == 0)
                {
                    return baseQuery;
                }

                // Le score fondamental n'est pas persiste : il est recalcule a la demande par
                // IFundamentalScoringService, exactement comme pour l'affichage de la colonne "Score
                // fondamental" du screener (voir screener-page.component.ts). On reutilise ici les memes
                // parametres implicites que le front (MinCategoriesRequired par defaut, pas de classement)
                // pour garantir que le score qui filtre est toujours celui qui est affiche.
                var fundamentalScoringService = _serviceProvider.GetRequiredService<IFundamentalScoringService>();
                var scoreResponse = await fundamentalScoringService.ScoreAsync(new FundamentalScoreRequest
                {
                    UniverseId = FundamentalScoringPolicyDefaults.SupportedUniverseId,
                    Symbols = candidateSymbols,
                    MinCategoriesRequired = FundamentalScoringPolicyDefaults.MinimumCategoriesRequiredDefault,
                    IncludeRankPosition = false
                }, ct);

                // Un actif sans score utilisable (hors univers PEA confirme eligible, couverture de
                // categories insuffisante, fondamentaux indisponibles) ne peut pas satisfaire "score >= X" :
                // il est exclu, exactement comme un actif sans TrailingPE l'est deja pour MinPE/MaxPE.
                var matchingSymbols = scoreResponse.Results
                    .Where(r => r.UsableScore && r.TotalScore.HasValue && r.TotalScore.Value >= query.MinScore.Value)
                    .Select(r => r.Symbol)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);

                baseQuery = baseQuery.Where(a => matchingSymbols.Contains(a.Symbol));
            }

            return baseQuery;
        }

        /// <summary>
        /// Ne conserve que le snapshot le plus récent par actif, quel que soit le type de snapshot
        /// (cotation, fondamentaux, etc.), en s'appuyant sur une colonne d'horodatage commune.
        /// </summary>
        private static IQueryable<TSnapshot> GetLatestByAssetId<TSnapshot>(IQueryable<TSnapshot> snapshots)
            where TSnapshot : class, IAssetSnapshot
        {
            var latest = snapshots
                .AsNoTracking()
                .GroupBy(s => s.AssetId)
                .Select(g => new { AssetId = g.Key, LastAsOfUtc = g.Max(s => s.AsOfUtc) });

            return snapshots
                .AsNoTracking()
                .Join(latest,
                    s => new { s.AssetId, s.AsOfUtc },
                    l => new { l.AssetId, AsOfUtc = l.LastAsOfUtc },
                    (s, l) => s);
        }

        private IQueryable<ScreenerItemViewModel> ProjectWithQuoteAndPea(IQueryable<Asset> assets)
        {
            var quotes = GetLatestByAssetId(_financeDbContext.AssetQuoteSnapshots);
            var fundamentals = GetLatestByAssetId(_financeDbContext.AssetFundamentalsSnapshots);

            var peaEligibleIds = _financeDbContext.AssetPeaEligibilities
                .AsNoTracking()
                .Where(p => p.EligibilityStatus == PeaEligibilityStatusEnum.ConfirmedEligible)
                .Select(p => p.AssetId);

            return assets
                .GroupJoin(quotes, a => a.Id, q => q.AssetId, (a, qs) => new { Asset = a, Quotes = qs })
                .SelectMany(x => x.Quotes.DefaultIfEmpty(), (x, q) => new { x.Asset, Quote = q })
                .GroupJoin(fundamentals, x => x.Asset.Id, f => f.AssetId, (x, fs) => new { x.Asset, x.Quote, Fundamentals = fs })
                .SelectMany(x => x.Fundamentals.DefaultIfEmpty(), (x, f) => new ScreenerItemViewModel
                {
                    Symbol = x.Asset.Symbol,
                    Name = x.Asset.Name ?? string.Empty,
                    Exchange = x.Asset.Exchange,
                    Country = x.Asset.Country,
                    Sector = x.Asset.Sector,
                    AssetType = (int)x.Asset.AssetType,
                    IsPeaEligible = peaEligibleIds.Contains(x.Asset.Id),
                    LastPrice = x.Quote == null ? (decimal?)null : x.Quote.LastPrice,
                    DayVariationPct = x.Quote == null ? (decimal?)null : x.Quote.DayVariationPct,
                    QuoteAsOfUtc = x.Quote == null ? (DateTime?)null : x.Quote.AsOfUtc,
                    TrailingPE = f == null ? (decimal?)null : f.TrailingPE,
                    DividendYield = f == null ? (decimal?)null : f.DividendYield,
                    MarketCap = f == null ? (decimal?)null : f.MarketCap
                });
        }

        private static IQueryable<ScreenerItemViewModel> ApplySort(IQueryable<ScreenerItemViewModel> query, string? sortBy, string? sortDirection)
        {
            var normalizedSortBy = sortBy is not null && SortWhitelist.Contains(sortBy) ? sortBy : "Symbol";
            var sortDesc = string.Equals(sortDirection, "desc", StringComparison.OrdinalIgnoreCase);

            return normalizedSortBy switch
            {
                "Name" => sortDesc ? query.OrderByDescending(i => i.Name) : query.OrderBy(i => i.Name),
                "Exchange" => sortDesc ? query.OrderByDescending(i => i.Exchange) : query.OrderBy(i => i.Exchange),
                "Sector" => sortDesc ? query.OrderByDescending(i => i.Sector) : query.OrderBy(i => i.Sector),
                "Country" => sortDesc ? query.OrderByDescending(i => i.Country) : query.OrderBy(i => i.Country),
                "AssetType" => sortDesc ? query.OrderByDescending(i => i.AssetType) : query.OrderBy(i => i.AssetType),
                "LastPrice" => sortDesc ? query.OrderByDescending(i => i.LastPrice) : query.OrderBy(i => i.LastPrice),
                "DayVariationPct" => sortDesc ? query.OrderByDescending(i => i.DayVariationPct) : query.OrderBy(i => i.DayVariationPct),
                "TrailingPE" => sortDesc ? query.OrderByDescending(i => i.TrailingPE) : query.OrderBy(i => i.TrailingPE),
                "DividendYield" => sortDesc ? query.OrderByDescending(i => i.DividendYield) : query.OrderBy(i => i.DividendYield),
                "MarketCap" => sortDesc ? query.OrderByDescending(i => i.MarketCap) : query.OrderBy(i => i.MarketCap),
                _ => sortDesc ? query.OrderByDescending(i => i.Symbol) : query.OrderBy(i => i.Symbol)
            };
        }

        public async Task<List<ScreenerPresetViewModel>> GetPresetsAsync(CancellationToken ct = default)
        {
            var userId = _currentUserId;
            if (string.IsNullOrWhiteSpace(userId))
            {
                return [];
            }

            var presets = await _financeDbContext.UserScreenerPresets
                .AsNoTracking()
                .Where(p => p.UserId == userId)
                .OrderByDescending(p => p.CreatedAtUtc)
                .ToListAsync(ct);

            return presets.Select(MapToPresetViewModel).ToList();
        }

        public async Task<ScreenerPresetViewModel> SavePresetAsync(ScreenerPresetCreateViewModel model, CancellationToken ct = default)
        {
            var userId = _currentUserId;
            if (string.IsNullOrWhiteSpace(userId))
            {
                throw new KeyNotFoundException("Utilisateur introuvable.");
            }

            var preset = new UserScreenerPreset
            {
                UserId = userId,
                Name = model.Name.Trim(),
                QueryJson = JsonSerializer.Serialize(model.Query),
                CreatedAtUtc = DateTime.UtcNow
            };

            _financeDbContext.UserScreenerPresets.Add(preset);
            await _financeDbContext.SaveChangesAsync(ct);

            return MapToPresetViewModel(preset);
        }

        public async Task DeletePresetAsync(string presetId, CancellationToken ct = default)
        {
            var userId = _currentUserId;

            var preset = await _financeDbContext.UserScreenerPresets
                .FirstOrDefaultAsync(p => p.Id == presetId && p.UserId == userId, ct)
                ?? throw new KeyNotFoundException($"Preset {presetId} introuvable.");

            preset.IsDeleted = true;
            preset.UpdatedAtUtc = DateTime.UtcNow;
            await _financeDbContext.SaveChangesAsync(ct);
        }

        private static ScreenerPresetViewModel MapToPresetViewModel(UserScreenerPreset preset)
        {
            return new ScreenerPresetViewModel
            {
                Id = preset.Id,
                Name = preset.Name,
                Query = JsonSerializer.Deserialize<ScreenerQueryViewModel>(preset.QueryJson) ?? new ScreenerQueryViewModel()
            };
        }
    }
}
