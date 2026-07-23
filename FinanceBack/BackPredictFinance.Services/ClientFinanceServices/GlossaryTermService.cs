using BackPredictFinance.ViewModels.ClientFinanceViewModels.Readings;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Text;

namespace BackPredictFinance.Services.ClientFinanceServices
{
    /// <summary>
    /// Expose les termes du glossaire produits au client et à l'admin.
    /// </summary>
    public interface IGlossaryTermService
    {
        /// <summary>
        /// Recherche les termes actifs et publiés. Si search est vide, retourne tous les termes triés alphabétiquement.
        /// Sinon, filtre sur NormalizedTerm contenant la valeur normalisée de search.
        /// </summary>
        Task<List<GlossaryProductTermViewModel>> SearchAsync(string? search, CancellationToken ct = default);
    }

    public sealed class GlossaryTermService : BaseService, IGlossaryTermService
    {
        public GlossaryTermService(IServiceProvider serviceProvider)
            : base(serviceProvider)
        {
        }

        public async Task<List<GlossaryProductTermViewModel>> SearchAsync(string? search, CancellationToken ct = default)
        {
            var query = _financeDbContext.GlossaryTerms
                .AsNoTracking()
                .Where(t => t.IsActive && t.IsPublished);

            if (!string.IsNullOrWhiteSpace(search))
            {
                var normalized = Normalize(search);
                query = query.Where(t => t.NormalizedTerm.Contains(normalized));
            }

            var terms = await query
                .OrderBy(t => t.Term)
                .ToListAsync(ct);

            return _mapper.Map<List<GlossaryProductTermViewModel>>(terms);
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
