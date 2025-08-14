using BackPredictFinance.Datas.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace BackPredictFinance.Services
{
    public interface IAssetService
    {
        Task<Asset> CreateAssetAsync(Asset asset);
        Task<Asset?> GetAssetByIdAsync(string assetId);
        Task<IEnumerable<Asset>> GetAllAssetsAsync();
        Task<Asset> UpdateAssetAsync(Asset asset);
        Task DeleteAssetAsync(string assetId);
    }
    public class AssetService : BaseService, IAssetService
    {
        public AssetService(IServiceProvider serviceProvider)
            : base(serviceProvider) { }

        public async Task<Asset> CreateAssetAsync(Asset asset)
        {
            asset.Id = asset.Id ?? Guid.NewGuid().ToString();
            await FinanceDbContext.Assets.AddAsync(asset);
            await FinanceDbContext.SaveChangesAsync();
            return asset;
        }

        public async Task<Asset?> GetAssetByIdAsync(string assetId)
        {
            return await FinanceDbContext.Assets
                .Include(a => a.Prices)
                .Include(a => a.PriceHistories)
                .FirstOrDefaultAsync(a => a.Id == assetId);
        }

        public async Task<IEnumerable<Asset>> GetAllAssetsAsync()
        {
            return await FinanceDbContext.Assets
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<Asset> UpdateAssetAsync(Asset asset)
        {
            FinanceDbContext.Assets.Update(asset);
            await FinanceDbContext.SaveChangesAsync();
            return asset;
        }

        public async Task DeleteAssetAsync(string assetId)
        {
            var asset = await FinanceDbContext.Assets.FindAsync(assetId);
            if (asset != null)
            {
                FinanceDbContext.Assets.Remove(asset);
                await FinanceDbContext.SaveChangesAsync();
            }
        }
    }
}
