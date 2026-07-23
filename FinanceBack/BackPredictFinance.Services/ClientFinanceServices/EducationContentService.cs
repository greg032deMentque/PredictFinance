using BackPredictFinance.ViewModels.ClientFinanceViewModels.Education;
using Microsoft.EntityFrameworkCore;

namespace BackPredictFinance.Services.ClientFinanceServices
{
    /// <summary>
    /// Expose les articles éducatifs publiés au client.
    /// </summary>
    public interface IEducationContentService
    {
        /// <summary>Retourne tous les articles actifs et publiés, triés par DisplayOrder.</summary>
        Task<List<EducationArticleSummaryViewModel>> GetPublishedAsync(CancellationToken ct = default);

        /// <summary>Retourne le détail d'un article par son slug, ou null si introuvable/non publié.</summary>
        Task<EducationArticleDetailViewModel?> GetBySlugAsync(string slug, CancellationToken ct = default);
    }

    public sealed class EducationContentService : BaseService, IEducationContentService
    {
        public EducationContentService(IServiceProvider serviceProvider)
            : base(serviceProvider)
        {
        }

        public async Task<List<EducationArticleSummaryViewModel>> GetPublishedAsync(CancellationToken ct = default)
        {
            var articles = await _financeDbContext.EducationArticles
                .AsNoTracking()
                .Where(a => a.IsActive && a.IsPublished)
                .OrderBy(a => a.DisplayOrder)
                .ToListAsync(ct);

            return _mapper.Map<List<EducationArticleSummaryViewModel>>(articles);
        }

        public async Task<EducationArticleDetailViewModel?> GetBySlugAsync(string slug, CancellationToken ct = default)
        {
            var article = await _financeDbContext.EducationArticles
                .AsNoTracking()
                .Where(a => a.IsActive && a.IsPublished && a.Slug == slug)
                .FirstOrDefaultAsync(ct);

            return article is null ? null : _mapper.Map<EducationArticleDetailViewModel>(article);
        }
    }
}
