using System.Net;
using BackPredictFinance.Common;
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
                throw new ArgumentException("La quantite doit etre strictement positive.", nameof(request));
            }

            if (request.UnitPrice <= 0m)
            {
                throw new ArgumentException("Le prix unitaire doit etre strictement positif.", nameof(request));
            }

            if (request.Fees < 0m)
            {
                throw new ArgumentException("Les frais ne peuvent pas etre negatifs.", nameof(request));
            }

            var symbol = _assetSupportService.NormalizeSymbol(request.Symbol);
            if (string.IsNullOrWhiteSpace(symbol))
            {
                throw new ArgumentException("Le symbole est obligatoire.", nameof(request));
            }

            var userId = _assetSupportService.GetRequiredCurrentUserId();
            var portfolio = await _portfolioService.GetRequiredPortfolioForUserAsync(request.PortfolioId, userId, ct);

            if (portfolio.Status == PortfolioStatusEnum.Archived)
            {
                throw new CustomException(
                    "Transaction refusée : portefeuille archivé.",
                    "Impossible d'ajouter une transaction dans un portefeuille archivé.",
                    statusCode: HttpStatusCode.UnprocessableEntity);
            }

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
                throw new CustomException(
                    $"Vente refusée : quantité demandée ({request.Quantity}) supérieure à la quantité détenue ({userAsset.Quantity}).",
                    $"Quantité insuffisante pour vendre. Vous détenez {userAsset.Quantity} action(s).",
                    statusCode: HttpStatusCode.UnprocessableEntity);
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
                .Where(x => x.UserAsset.UserId == _currentUserId && !x.IsDeleted);

            if (!string.IsNullOrWhiteSpace(portfolioId))
                query = query.Where(x => x.PortfolioId == portfolioId);
            else
                query = query.ExcludeArchivedPortfolios();

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
                .FirstOrDefaultAsync(x => x.Id == transactionId && x.UserAsset.UserId == _currentUserId && !x.IsDeleted, ct);

            if (transaction == null)
            {
                return;
            }

            var userAsset = transaction.UserAsset;

            if (transaction.TransactionType == TransactionTypeEnum.Buy)
            {
                if (userAsset.Quantity < transaction.Quantity)
                {
                    throw new CustomException(
                        "Suppression refusée : solde insuffisant pour annuler cet achat.",
                        "Suppression impossible : la quantité actuelle est insuffisante pour annuler cet achat.",
                        statusCode: HttpStatusCode.UnprocessableEntity);
                }

                userAsset.Quantity -= transaction.Quantity;
            }
            else
            {
                userAsset.Quantity += transaction.Quantity;
            }

            transaction.IsDeleted = true;
            await _financeDbContext.SaveChangesAsync(ct);
        }
    }
}
