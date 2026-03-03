using BackPredictFinance.Datas.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace BackPredictFinance.Services.UserServices
{
    public interface IUserAssetService
    {
        Task<UserAsset> AddUserAssetAsync(string userId, string assetId, decimal quantity);
        Task<UserAsset?> GetUserAssetAsync(string userId, string assetId);
        Task<IEnumerable<UserAsset>> GetUserAssetsAsync(string userId);
        Task<UserAsset> UpdateUserAssetQuantityAsync(string userId, string assetId, decimal quantity);
        Task RemoveUserAssetAsync(string userId, string assetId);
    }
    public class UserAssetService : BaseService, IUserAssetService
    {
        public UserAssetService(IServiceProvider serviceProvider)
            : base(serviceProvider) { }

        public async Task<UserAsset> AddUserAssetAsync(string userId, string assetId, decimal quantity)
        {
            var entity = new UserAsset
            {
                UserId = userId,
                AssetId = assetId,
                Quantity = quantity
            };
            await _financeDbContext.UserAssets.AddAsync(entity);
            await _financeDbContext.SaveChangesAsync();
            return entity;
        }

        public async Task<UserAsset?> GetUserAssetAsync(string userId, string assetId)
        {
            return await _financeDbContext.UserAssets
                .Include(ua => ua.Asset)
                .FirstOrDefaultAsync(ua => ua.UserId == userId && ua.AssetId == assetId);
        }

        public async Task<IEnumerable<UserAsset>> GetUserAssetsAsync(string userId)
        {
            return await _financeDbContext.UserAssets
                .Include(ua => ua.Asset)
                .Where(ua => ua.UserId == userId)
                .ToListAsync();
        }

        public async Task<UserAsset> UpdateUserAssetQuantityAsync(string userId, string assetId, decimal quantity)
        {
            var entity = await GetUserAssetAsync(userId, assetId);
            if (entity == null)
                throw new InvalidOperationException("UserAsset not found.");

            entity.Quantity = quantity;
            _financeDbContext.UserAssets.Update(entity);
            await _financeDbContext.SaveChangesAsync();
            return entity;
        }

        public async Task RemoveUserAssetAsync(string userId, string assetId)
        {
            var entity = await GetUserAssetAsync(userId, assetId);
            if (entity != null)
            {
                _financeDbContext.UserAssets.Remove(entity);
                await _financeDbContext.SaveChangesAsync();
            }
        }
    }
}
