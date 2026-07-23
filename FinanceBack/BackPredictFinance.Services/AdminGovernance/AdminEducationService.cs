using BackPredictFinance.Datas.Entities;
using BackPredictFinance.ViewModels.AdminViewModels.Education;
using Microsoft.EntityFrameworkCore;

namespace BackPredictFinance.Services.AdminGovernance
{
    public interface IAdminEducationService
    {
        Task<List<EducationArticleAdminViewModel>> GetAllAsync(CancellationToken ct = default);
        Task<EducationArticleAdminViewModel?> GetByIdAsync(string id, CancellationToken ct = default);
        Task<EducationArticleAdminViewModel> CreateAsync(EducationArticleUpsertRequestViewModel request, CancellationToken ct = default);
        Task<EducationArticleAdminViewModel> UpdateAsync(string id, EducationArticleUpsertRequestViewModel request, CancellationToken ct = default);
        Task DeleteAsync(string id, CancellationToken ct = default);
    }

    public sealed class AdminEducationService : BaseService, IAdminEducationService
    {
        public AdminEducationService(IServiceProvider serviceProvider) : base(serviceProvider)
        {
        }

        public async Task<List<EducationArticleAdminViewModel>> GetAllAsync(CancellationToken ct = default)
        {
            var articles = await _financeDbContext.EducationArticles
                .AsNoTracking()
                .OrderBy(a => a.DisplayOrder)
                .ToListAsync(ct);

            return _mapper.Map<List<EducationArticleAdminViewModel>>(articles);
        }

        public async Task<EducationArticleAdminViewModel?> GetByIdAsync(string id, CancellationToken ct = default)
        {
            var article = await _financeDbContext.EducationArticles
                .AsNoTracking()
                .Where(a => a.Id == id)
                .FirstOrDefaultAsync(ct);

            return article is null ? null : _mapper.Map<EducationArticleAdminViewModel>(article);
        }

        public async Task<EducationArticleAdminViewModel> CreateAsync(EducationArticleUpsertRequestViewModel request, CancellationToken ct = default)
        {
            var article = new EducationArticle
            {
                Id = Guid.NewGuid().ToString(),
                Slug = request.Slug,
                ProductType = request.ProductType,
                Title = request.Title,
                Summary = request.Summary,
                BodyMarkdown = request.BodyMarkdown,
                DisplayOrder = request.DisplayOrder,
                IsPublished = request.IsPublished,
                IsActive = true,
                IsDeleted = false
            };

            _financeDbContext.EducationArticles.Add(article);
            await _financeDbContext.SaveChangesAsync(ct);

            return _mapper.Map<EducationArticleAdminViewModel>(article);
        }

        public async Task<EducationArticleAdminViewModel> UpdateAsync(string id, EducationArticleUpsertRequestViewModel request, CancellationToken ct = default)
        {
            var article = await _financeDbContext.EducationArticles
                .Where(a => a.Id == id)
                .FirstOrDefaultAsync(ct);

            if (article is null)
                throw new KeyNotFoundException($"Article {id} introuvable.");

            article.Slug = request.Slug;
            article.ProductType = request.ProductType;
            article.Title = request.Title;
            article.Summary = request.Summary;
            article.BodyMarkdown = request.BodyMarkdown;
            article.DisplayOrder = request.DisplayOrder;
            article.IsPublished = request.IsPublished;

            await _financeDbContext.SaveChangesAsync(ct);

            return _mapper.Map<EducationArticleAdminViewModel>(article);
        }

        public async Task DeleteAsync(string id, CancellationToken ct = default)
        {
            var article = await _financeDbContext.EducationArticles
                .Where(a => a.Id == id)
                .FirstOrDefaultAsync(ct);

            if (article is null)
                throw new KeyNotFoundException($"Article {id} introuvable.");

            article.IsDeleted = true;
            await _financeDbContext.SaveChangesAsync(ct);
        }
    }
}
