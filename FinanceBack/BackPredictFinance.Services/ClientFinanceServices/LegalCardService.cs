using BackPredictFinance.Datas.Entities;
using BackPredictFinance.ViewModels.AdminViewModels.Content;
using BackPredictFinance.ViewModels.ClientFinanceViewModels.Content;
using Microsoft.EntityFrameworkCore;

namespace BackPredictFinance.Services.ClientFinanceServices
{
    public interface ILegalCardService
    {
        Task<List<LegalCardViewModel>> GetPublishedAsync(CancellationToken ct = default);
        Task<List<LegalCardAdminViewModel>> GetAllAdminAsync(CancellationToken ct = default);
        Task<LegalCardAdminViewModel?> GetByIdAsync(string id, CancellationToken ct = default);
        Task<LegalCardAdminViewModel> CreateAsync(LegalCardUpsertRequestViewModel request, CancellationToken ct = default);
        Task<LegalCardAdminViewModel> UpdateAsync(string id, LegalCardUpsertRequestViewModel request, CancellationToken ct = default);
        Task DeleteAsync(string id, CancellationToken ct = default);
    }

    public sealed class LegalCardService : BaseService, ILegalCardService
    {
        public LegalCardService(IServiceProvider serviceProvider) : base(serviceProvider)
        {
        }

        public async Task<List<LegalCardViewModel>> GetPublishedAsync(CancellationToken ct = default)
        {
            var cards = await _financeDbContext.LegalCards
                .AsNoTracking()
                .Where(c => c.IsActive && c.IsPublished)
                .OrderBy(c => c.DisplayOrder)
                .ToListAsync(ct);

            return _mapper.Map<List<LegalCardViewModel>>(cards);
        }

        public async Task<List<LegalCardAdminViewModel>> GetAllAdminAsync(CancellationToken ct = default)
        {
            var cards = await _financeDbContext.LegalCards
                .AsNoTracking()
                .OrderBy(c => c.DisplayOrder)
                .ToListAsync(ct);

            return _mapper.Map<List<LegalCardAdminViewModel>>(cards);
        }

        public async Task<LegalCardAdminViewModel?> GetByIdAsync(string id, CancellationToken ct = default)
        {
            var card = await _financeDbContext.LegalCards
                .AsNoTracking()
                .Where(c => c.Id == id)
                .FirstOrDefaultAsync(ct);

            return card is null ? null : _mapper.Map<LegalCardAdminViewModel>(card);
        }

        public async Task<LegalCardAdminViewModel> CreateAsync(LegalCardUpsertRequestViewModel request, CancellationToken ct = default)
        {
            var card = new LegalCard
            {
                Id = Guid.NewGuid().ToString(),
                Key = request.Key,
                Icon = request.Icon,
                Title = request.Title,
                Description = request.Description,
                EffectiveDate = request.EffectiveDate,
                TargetRoute = request.TargetRoute,
                DisplayOrder = request.DisplayOrder,
                IsPublished = request.IsPublished,
                IsActive = true,
                IsDeleted = false
            };

            _financeDbContext.LegalCards.Add(card);
            await _financeDbContext.SaveChangesAsync(ct);

            return _mapper.Map<LegalCardAdminViewModel>(card);
        }

        public async Task<LegalCardAdminViewModel> UpdateAsync(string id, LegalCardUpsertRequestViewModel request, CancellationToken ct = default)
        {
            var card = await _financeDbContext.LegalCards
                .Where(c => c.Id == id)
                .FirstOrDefaultAsync(ct);

            if (card is null)
                throw new KeyNotFoundException($"Legal card {id} introuvable.");

            card.Key = request.Key;
            card.Icon = request.Icon;
            card.Title = request.Title;
            card.Description = request.Description;
            card.EffectiveDate = request.EffectiveDate;
            card.TargetRoute = request.TargetRoute;
            card.DisplayOrder = request.DisplayOrder;
            card.IsPublished = request.IsPublished;

            await _financeDbContext.SaveChangesAsync(ct);

            return _mapper.Map<LegalCardAdminViewModel>(card);
        }

        public async Task DeleteAsync(string id, CancellationToken ct = default)
        {
            var card = await _financeDbContext.LegalCards
                .Where(c => c.Id == id)
                .FirstOrDefaultAsync(ct);

            if (card is null)
                throw new KeyNotFoundException($"Legal card {id} introuvable.");

            card.IsDeleted = true;
            await _financeDbContext.SaveChangesAsync(ct);
        }
    }
}
