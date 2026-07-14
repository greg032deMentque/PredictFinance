using BackPredictFinance.Datas.Entities;
using BackPredictFinance.ViewModels.AdminViewModels.Content;
using BackPredictFinance.ViewModels.ClientFinanceViewModels.Content;
using Microsoft.EntityFrameworkCore;

namespace BackPredictFinance.Services.ClientFinanceServices
{
    public interface IFaqService
    {
        Task<List<FaqEntryViewModel>> GetPublishedAsync(CancellationToken ct = default);
        Task<List<FaqEntryAdminViewModel>> GetAllAdminAsync(CancellationToken ct = default);
        Task<FaqEntryAdminViewModel?> GetByIdAsync(string id, CancellationToken ct = default);
        Task<FaqEntryAdminViewModel> CreateAsync(FaqEntryUpsertRequestViewModel request, CancellationToken ct = default);
        Task<FaqEntryAdminViewModel> UpdateAsync(string id, FaqEntryUpsertRequestViewModel request, CancellationToken ct = default);
        Task DeleteAsync(string id, CancellationToken ct = default);
    }

    public sealed class FaqService : BaseService, IFaqService
    {
        public FaqService(IServiceProvider serviceProvider) : base(serviceProvider)
        {
        }

        public async Task<List<FaqEntryViewModel>> GetPublishedAsync(CancellationToken ct = default)
        {
            var entries = await _financeDbContext.FaqEntries
                .AsNoTracking()
                .Where(e => e.IsActive && e.IsPublished && !e.IsDeleted)
                .OrderBy(e => e.DisplayOrder)
                .ToListAsync(ct);

            return _mapper.Map<List<FaqEntryViewModel>>(entries);
        }

        public async Task<List<FaqEntryAdminViewModel>> GetAllAdminAsync(CancellationToken ct = default)
        {
            var entries = await _financeDbContext.FaqEntries
                .AsNoTracking()
                .Where(e => !e.IsDeleted)
                .OrderBy(e => e.DisplayOrder)
                .ToListAsync(ct);

            return _mapper.Map<List<FaqEntryAdminViewModel>>(entries);
        }

        public async Task<FaqEntryAdminViewModel?> GetByIdAsync(string id, CancellationToken ct = default)
        {
            var entry = await _financeDbContext.FaqEntries
                .AsNoTracking()
                .Where(e => !e.IsDeleted && e.Id == id)
                .FirstOrDefaultAsync(ct);

            return entry is null ? null : _mapper.Map<FaqEntryAdminViewModel>(entry);
        }

        public async Task<FaqEntryAdminViewModel> CreateAsync(FaqEntryUpsertRequestViewModel request, CancellationToken ct = default)
        {
            var entry = new FaqEntry
            {
                Id = Guid.NewGuid().ToString(),
                Category = request.Category,
                Question = request.Question,
                Answer = request.Answer,
                DisplayOrder = request.DisplayOrder,
                IsPublished = request.IsPublished,
                IsActive = true,
                IsDeleted = false
            };

            _financeDbContext.FaqEntries.Add(entry);
            await _financeDbContext.SaveChangesAsync(ct);

            return _mapper.Map<FaqEntryAdminViewModel>(entry);
        }

        public async Task<FaqEntryAdminViewModel> UpdateAsync(string id, FaqEntryUpsertRequestViewModel request, CancellationToken ct = default)
        {
            var entry = await _financeDbContext.FaqEntries
                .Where(e => !e.IsDeleted && e.Id == id)
                .FirstOrDefaultAsync(ct);

            if (entry is null)
                throw new KeyNotFoundException($"FAQ entry {id} introuvable.");

            entry.Category = request.Category;
            entry.Question = request.Question;
            entry.Answer = request.Answer;
            entry.DisplayOrder = request.DisplayOrder;
            entry.IsPublished = request.IsPublished;

            await _financeDbContext.SaveChangesAsync(ct);

            return _mapper.Map<FaqEntryAdminViewModel>(entry);
        }

        public async Task DeleteAsync(string id, CancellationToken ct = default)
        {
            var entry = await _financeDbContext.FaqEntries
                .Where(e => !e.IsDeleted && e.Id == id)
                .FirstOrDefaultAsync(ct);

            if (entry is null)
                throw new KeyNotFoundException($"FAQ entry {id} introuvable.");

            entry.IsDeleted = true;
            await _financeDbContext.SaveChangesAsync(ct);
        }
    }
}
