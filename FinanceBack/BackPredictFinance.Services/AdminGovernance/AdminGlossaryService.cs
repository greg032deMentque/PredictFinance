using BackPredictFinance.Datas.Entities;
using BackPredictFinance.ViewModels.AdminViewModels.Education;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Text;

namespace BackPredictFinance.Services.AdminGovernance
{
    public interface IAdminGlossaryService
    {
        Task<List<GlossaryTermAdminViewModel>> GetAllAsync(CancellationToken ct = default);
        Task<GlossaryTermAdminViewModel?> GetByIdAsync(string id, CancellationToken ct = default);
        Task<GlossaryTermAdminViewModel> CreateAsync(GlossaryTermUpsertRequestViewModel request, CancellationToken ct = default);
        Task<GlossaryTermAdminViewModel> UpdateAsync(string id, GlossaryTermUpsertRequestViewModel request, CancellationToken ct = default);
        Task DeleteAsync(string id, CancellationToken ct = default);
    }

    public sealed class AdminGlossaryService : BaseService, IAdminGlossaryService
    {
        public AdminGlossaryService(IServiceProvider serviceProvider) : base(serviceProvider)
        {
        }

        public async Task<List<GlossaryTermAdminViewModel>> GetAllAsync(CancellationToken ct = default)
        {
            var terms = await _financeDbContext.GlossaryTerms
                .AsNoTracking()
                .OrderBy(t => t.Term)
                .ToListAsync(ct);

            return _mapper.Map<List<GlossaryTermAdminViewModel>>(terms);
        }

        public async Task<GlossaryTermAdminViewModel?> GetByIdAsync(string id, CancellationToken ct = default)
        {
            var term = await _financeDbContext.GlossaryTerms
                .AsNoTracking()
                .Where(t => t.Id == id)
                .FirstOrDefaultAsync(ct);

            return term is null ? null : _mapper.Map<GlossaryTermAdminViewModel>(term);
        }

        public async Task<GlossaryTermAdminViewModel> CreateAsync(GlossaryTermUpsertRequestViewModel request, CancellationToken ct = default)
        {
            var term = new GlossaryTerm
            {
                Id = Guid.NewGuid().ToString(),
                Term = request.Term.Trim(),
                NormalizedTerm = Normalize(request.Term),
                Definition = request.Definition,
                Category = request.Category,
                IsPublished = request.IsPublished,
                IsActive = true,
                IsDeleted = false
            };

            _financeDbContext.GlossaryTerms.Add(term);
            await _financeDbContext.SaveChangesAsync(ct);

            return _mapper.Map<GlossaryTermAdminViewModel>(term);
        }

        public async Task<GlossaryTermAdminViewModel> UpdateAsync(string id, GlossaryTermUpsertRequestViewModel request, CancellationToken ct = default)
        {
            var term = await _financeDbContext.GlossaryTerms
                .Where(t => t.Id == id)
                .FirstOrDefaultAsync(ct);

            if (term is null)
                throw new KeyNotFoundException($"Terme {id} introuvable.");

            term.Term = request.Term.Trim();
            term.NormalizedTerm = Normalize(request.Term);
            term.Definition = request.Definition;
            term.Category = request.Category;
            term.IsPublished = request.IsPublished;

            await _financeDbContext.SaveChangesAsync(ct);

            return _mapper.Map<GlossaryTermAdminViewModel>(term);
        }

        public async Task DeleteAsync(string id, CancellationToken ct = default)
        {
            var term = await _financeDbContext.GlossaryTerms
                .Where(t => t.Id == id)
                .FirstOrDefaultAsync(ct);

            if (term is null)
                throw new KeyNotFoundException($"Terme {id} introuvable.");

            term.IsDeleted = true;
            await _financeDbContext.SaveChangesAsync(ct);
        }

        private static string Normalize(string value)
        {
            var trimmed = value.Trim().ToLowerInvariant();
            var normalized = trimmed.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder(normalized.Length);
            foreach (var c in normalized)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                    sb.Append(c);
            }
            return sb.ToString().Normalize(NormalizationForm.FormC);
        }
    }
}
