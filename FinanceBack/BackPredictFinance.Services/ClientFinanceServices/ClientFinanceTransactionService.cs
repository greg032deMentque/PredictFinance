using BackPredictFinance.Common.enums;
using BackPredictFinance.Datas.Entities;
using BackPredictFinance.ViewModels.ClientFinanceViewModels.Transactions;
using Microsoft.EntityFrameworkCore;

namespace BackPredictFinance.Services.ClientFinanceServices
{
    public interface IClientFinanceTransactionService
    {
        Task<TransactionItemViewModel> RegisterTransactionAsync(TransactionCreateRequestViewModel request, CancellationToken ct = default);
        Task<List<TransactionItemViewModel>> GetTransactionsAsync(int take, string? portfolioId = null, CancellationToken ct = default);
        Task DeleteTransactionAsync(string transactionId, CancellationToken ct = default);
    }

    public sealed class ClientFinanceTransactionService : BaseService, IClientFinanceTransactionService
    {
        private readonly IClientFinanceAssetSupportService _assetSupportService;
        private readonly IPortfolioService _portfolioService;

        public ClientFinanceTransactionService(
            IServiceProvider serviceProvider,
            IClientFinanceAssetSupportService assetSupportService,
            IPortfolioService portfolioService)
            : base(serviceProvider)
        {
            _assetSupportService = assetSupportService;
            _portfolioService = portfolioService;
        }

        public async Task<TransactionItemViewModel> RegisterTransactionAsync(TransactionCreateRequestViewModel request, CancellationToken ct = default)
        {
            if (request.Quantity <= 0m)
            {
                throw new ArgumentException("La quantite doit etre strictement positive.", nameof(request.Quantity));
            }

            if (request.UnitPrice <= 0m)
            {
                throw new ArgumentException("Le prix unitaire doit etre strictement positif.", nameof(request.UnitPrice));
            }

            if (request.Fees < 0m)
            {
                throw new ArgumentException("Les frais ne peuvent pas etre negatifs.", nameof(request.Fees));
            }

            var symbol = _assetSupportService.NormalizeSymbol(request.Symbol);
            if (string.IsNullOrWhiteSpace(symbol))
            {
                throw new ArgumentException("Le symbole est obligatoire.", nameof(request.Symbol));
            }

            var userId = _assetSupportService.GetRequiredCurrentUserId();
            var portfolio = await _portfolioService.GetRequiredPortfolioForUserAsync(request.PortfolioId, userId, ct);

            var transactionType = request.TransactionType;
            var asset = await _assetSupportService.EnsureAssetAsync(symbol, symbol, ct);

            var userAsset = await _financeDbContext.UserAssets
                .FirstOrDefaultAsync(x => x.UserId == _currentUserId && x.AssetId == asset.Id, ct);

            if (userAsset == null)
            {
                userAsset = new UserAsset
                {
                    UserId = userId,
                    AssetId = asset.Id,
                    Quantity = 0m
                };

                await _financeDbContext.UserAssets.AddAsync(userAsset, ct);
            }

            if (transactionType == TransactionTypeEnum.Sell && userAsset.Quantity < request.Quantity)
            {
                throw new InvalidOperationException("Quantite insuffisante pour vendre.");
            }

            userAsset.Quantity = transactionType == TransactionTypeEnum.Buy
                ? userAsset.Quantity + request.Quantity
                : userAsset.Quantity - request.Quantity;

            var transaction = new AssetTransaction
            {
                UserAssetId = userAsset.Id,
                PortfolioId = request.PortfolioId,
                TimestampUtc = request.TimestampUtc?.ToUniversalTime() ?? DateTime.UtcNow,
                TransactionType = transactionType,
                Quantity = request.Quantity,
                UnitPrice = request.UnitPrice,
                Fees = request.Fees
            };

            await _financeDbContext.AssetTransactions.AddAsync(transaction, ct);
            await _financeDbContext.SaveChangesAsync(ct);

            var gross = request.Quantity * request.UnitPrice;
            var net = transactionType == TransactionTypeEnum.Buy ? gross + request.Fees : gross - request.Fees;

            return new TransactionItemViewModel
            {
                Id = transaction.Id,
                Symbol = asset.Symbol,
                CompanyName = asset.Name ?? asset.Symbol,
                TransactionType = transactionType.ToString(),
                Quantity = request.Quantity,
                UnitPrice = request.UnitPrice,
                Fees = request.Fees,
                GrossAmount = decimal.Round(gross, 2),
                NetAmount = decimal.Round(net, 2),
                TimestampUtc = transaction.TimestampUtc,
                PortfolioId = portfolio.Id,
                PortfolioName = portfolio.Name
            };
        }

        public async Task<List<TransactionItemViewModel>> GetTransactionsAsync(int take, string? portfolioId = null, CancellationToken ct = default)
        {
            var size = Math.Clamp(take, 1, 200);

            var query = _financeDbContext.AssetTransactions
                .AsNoTracking()
                .Include(x => x.UserAsset)
                .ThenInclude(x => x.Asset)
                .Include(x => x.Portfolio)
                .Where(x => x.UserAsset.UserId == _currentUserId);

            if (!string.IsNullOrWhiteSpace(portfolioId))
                query = query.Where(x => x.PortfolioId == portfolioId);

            var transactions = await query
                .OrderByDescending(x => x.TimestampUtc)
                .Take(size)
                .ToListAsync(ct);

            return _mapper.Map<List<TransactionItemViewModel>>(transactions);
        }

        public async Task DeleteTransactionAsync(string transactionId, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(transactionId))
            {
                throw new ArgumentException("L'identifiant de transaction est obligatoire.", nameof(transactionId));
            }

            var transaction = await _financeDbContext.AssetTransactions
                .Include(x => x.UserAsset)
                .ThenInclude(x => x.Asset)
                .FirstOrDefaultAsync(x => x.Id == transactionId && x.UserAsset.UserId == _currentUserId, ct);

            if (transaction == null)
            {
                return;
            }

            var userAsset = transaction.UserAsset;

            if (transaction.TransactionType == TransactionTypeEnum.Buy)
            {
                if (userAsset.Quantity < transaction.Quantity)
                {
                    throw new InvalidOperationException("Suppression impossible: quantite actuelle insuffisante.");
                }

                userAsset.Quantity -= transaction.Quantity;
            }
            else
            {
                userAsset.Quantity += transaction.Quantity;
            }

            _financeDbContext.AssetTransactions.Remove(transaction);
            await _financeDbContext.SaveChangesAsync(ct);
        }
    }
}
