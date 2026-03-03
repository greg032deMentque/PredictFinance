using BackPredictFinance.Datas.Entities;
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
            await _financeDbContext.Assets.AddAsync(asset);
            await _financeDbContext.SaveChangesAsync();
            return asset;
        }

        public async Task<Asset?> GetAssetByIdAsync(string assetId)
        {
            return await _financeDbContext.Assets
                .FirstOrDefaultAsync(a => a.Id == assetId);
        }

        public async Task<IEnumerable<Asset>> GetAllAssetsAsync()
        {
            return await _financeDbContext.Assets
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<Asset> UpdateAssetAsync(Asset asset)
        {
            _financeDbContext.Assets.Update(asset);
            await _financeDbContext.SaveChangesAsync();
            return asset;
        }

        public async Task DeleteAssetAsync(string assetId)
        {
            var asset = await _financeDbContext.Assets.FindAsync(assetId);
            if (asset != null)
            {
                _financeDbContext.Assets.Remove(asset);
                await _financeDbContext.SaveChangesAsync();
            }
        }
    }
}

