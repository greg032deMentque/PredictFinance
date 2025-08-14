using BackPredictFinance.Datas.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace BackPredictFinance.Services
{
    public interface IPriceHistoryService
    {
        Task<PriceHistory> LogPriceAsync(string assetId, DateTime timestampUtc, decimal price, decimal? volume = null);
        Task<IEnumerable<PriceHistory>> GetPriceHistoryAsync(string assetId, DateTime fromUtc, DateTime toUtc);
    }

    public class PriceHistoryService : BaseService, IPriceHistoryService
    {
        public PriceHistoryService(IServiceProvider serviceProvider)
            : base(serviceProvider) { }

        public async Task<PriceHistory> LogPriceAsync(string assetId, DateTime timestampUtc, decimal price, decimal? volume = null)
        {
            var entry = new PriceHistory
            {
                AssetId = assetId,
                RetrievedAtUtc = timestampUtc,
                Price = price,
                Volume = volume
            };
            await FinanceDbContext.PriceHistories.AddAsync(entry);
            await FinanceDbContext.SaveChangesAsync();
            return entry;
        }

        public async Task<IEnumerable<PriceHistory>> GetPriceHistoryAsync(string assetId, DateTime fromUtc, DateTime toUtc)
        {
            return await FinanceDbContext.PriceHistories
                .AsNoTracking()
                .Where(ph => ph.AssetId == assetId
                             && ph.RetrievedAtUtc >= fromUtc
                             && ph.RetrievedAtUtc <= toUtc)
                .OrderBy(ph => ph.RetrievedAtUtc)
                .ToListAsync();
        }
    }
}
