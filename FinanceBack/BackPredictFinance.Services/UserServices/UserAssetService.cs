using BackPredictFinance.Datas.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace BackPredictFinance.Services.UserServices
{
    /// <summary>
    /// Gère les lignes d'actifs rattachées à un utilisateur.
    /// </summary>
    public interface IUserAssetService
    {
        /// <summary>
        /// Ajoute une ligne d'actif à un utilisateur.
        /// </summary>
        Task<UserAsset> AddUserAssetAsync(string userId, string assetId, decimal quantity);
        /// <summary>
        /// Retourne une ligne utilisateur pour un actif donné.
        /// </summary>
        Task<UserAsset?> GetUserAssetAsync(string userId, string assetId);
        /// <summary>
        /// Retourne toutes les lignes d'actifs d'un utilisateur.
        /// </summary>
        Task<IEnumerable<UserAsset>> GetUserAssetsAsync(string userId);
        /// <summary>
        /// Met à jour la quantité détenue pour une ligne utilisateur.
        /// </summary>
        Task<UserAsset> UpdateUserAssetQuantityAsync(string userId, string assetId, decimal quantity);
        /// <summary>
        /// Supprime la ligne d'un actif pour un utilisateur.
        /// </summary>
        Task RemoveUserAssetAsync(string userId, string assetId);
    }
    /// <summary>
    /// Implémente la gestion des rattachements utilisateur-actif.
    /// </summary>
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
