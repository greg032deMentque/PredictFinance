using BackPredictFinance.Common;
using BackPredictFinance.Common.enums;
using BackPredictFinance.Datas.Entities;
using BackPredictFinance.ViewModels.ClientFinanceViewModels.Portfolios;
using Microsoft.EntityFrameworkCore;
using System.Net;

namespace BackPredictFinance.Services.ClientFinanceServices
{
    public interface IPortfolioService
    {
        Task<List<UserPortfolioViewModel>> GetPortfoliosAsync(CancellationToken ct = default);
        Task<UserPortfolioViewModel> CreatePortfolioAsync(PortfolioCreateRequestViewModel request, CancellationToken ct = default);
        Task<UserPortfolioViewModel> RenamePortfolioAsync(string portfolioId, PortfolioRenameRequestViewModel request, CancellationToken ct = default);
        Task ArchivePortfolioAsync(string portfolioId, CancellationToken ct = default);
        Task RestorePortfolioAsync(string portfolioId, CancellationToken ct = default);
        Task<Portfolio> GetRequiredPortfolioForUserAsync(string portfolioId, string userId, CancellationToken ct = default);
    }

    public sealed class PortfolioService : BaseService, IPortfolioService
    {
        public PortfolioService(IServiceProvider serviceProvider)
            : base(serviceProvider)
        {
        }

        public async Task<List<UserPortfolioViewModel>> GetPortfoliosAsync(CancellationToken ct = default)
        {
            var portfolios = await _financeDbContext.Portfolios
                .AsNoTracking()
                .Where(p => p.UserId == _currentUserId && !p.IsDeleted && p.Status == PortfolioStatusEnum.Active)
                .OrderBy(p => p.Name)
                .ToListAsync(ct);

            return _mapper.Map<List<UserPortfolioViewModel>>(portfolios);
        }

        public async Task<UserPortfolioViewModel> CreatePortfolioAsync(PortfolioCreateRequestViewModel request, CancellationToken ct = default)
        {
            if (!Enum.TryParse<PortfolioTypeEnum>(request.PortfolioType, ignoreCase: true, out var portfolioType))
            {
                throw new CustomException(
                    $"Invalid portfolio type: {request.PortfolioType}",
                    "Le type de portefeuille fourni est invalide.",
                    statusCode: HttpStatusCode.BadRequest);
            }

            var duplicate = await _financeDbContext.Portfolios
                .AsNoTracking()
                .AnyAsync(p => p.UserId == _currentUserId && p.Name == request.Name && !p.IsDeleted, ct);

            if (duplicate)
            {
                throw new CustomException(
                    $"Duplicate portfolio name: {request.Name}",
                    $"Un portefeuille nommé \"{request.Name}\" existe déjà.",
                    statusCode: HttpStatusCode.Conflict);
            }

            var portfolio = new Portfolio
            {
                UserId = _currentUserId!,
                Name = request.Name,
                PortfolioType = portfolioType,
                IsDeleted = false
            };

            await _financeDbContext.Portfolios.AddAsync(portfolio, ct);
            await _financeDbContext.SaveChangesAsync(ct);

            return _mapper.Map<UserPortfolioViewModel>(portfolio);
        }

        public async Task<UserPortfolioViewModel> RenamePortfolioAsync(string portfolioId, PortfolioRenameRequestViewModel request, CancellationToken ct = default)
        {
            var portfolio = await _financeDbContext.Portfolios
                .FirstOrDefaultAsync(p => p.Id == portfolioId && p.UserId == _currentUserId && !p.IsDeleted, ct);

            if (portfolio is null)
            {
                throw new KeyNotFoundException("Portefeuille introuvable.");
            }

            if (portfolio.Name == request.Name)
            {
                return _mapper.Map<UserPortfolioViewModel>(portfolio);
            }

            var duplicate = await _financeDbContext.Portfolios
                .AsNoTracking()
                .AnyAsync(p => p.UserId == _currentUserId && p.Name == request.Name && p.Id != portfolioId && !p.IsDeleted, ct);

            if (duplicate)
            {
                throw new CustomException(
                    $"Duplicate portfolio name on rename: {request.Name}",
                    $"Un portefeuille nommé \"{request.Name}\" existe déjà.",
                    statusCode: HttpStatusCode.Conflict);
            }

            portfolio.Name = request.Name;
            await _financeDbContext.SaveChangesAsync(ct);

            return _mapper.Map<UserPortfolioViewModel>(portfolio);
        }

        public async Task ArchivePortfolioAsync(string portfolioId, CancellationToken ct = default)
        {
            var portfolio = await _financeDbContext.Portfolios
                .FirstOrDefaultAsync(p => p.Id == portfolioId && p.UserId == _currentUserId && !p.IsDeleted, ct);

            if (portfolio is null)
            {
                return;
            }

            portfolio.Status = PortfolioStatusEnum.Archived;
            await _financeDbContext.SaveChangesAsync(ct);
        }

        public async Task RestorePortfolioAsync(string portfolioId, CancellationToken ct = default)
        {
            var portfolio = await _financeDbContext.Portfolios
                .FirstOrDefaultAsync(p => p.Id == portfolioId && p.UserId == _currentUserId && !p.IsDeleted, ct);

            if (portfolio is null)
            {
                return;
            }

            portfolio.Status = PortfolioStatusEnum.Active;
            await _financeDbContext.SaveChangesAsync(ct);
        }

        public async Task<Portfolio> GetRequiredPortfolioForUserAsync(string portfolioId, string userId, CancellationToken ct = default)
        {
            var portfolio = await _financeDbContext.Portfolios
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == portfolioId && p.UserId == userId && !p.IsDeleted, ct);

            if (portfolio is null)
            {
                throw new KeyNotFoundException("Portefeuille introuvable ou inaccessible.");
            }

            return portfolio;
        }
    }
}
