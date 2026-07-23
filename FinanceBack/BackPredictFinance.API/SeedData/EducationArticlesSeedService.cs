using BackPredictFinance.Common.enums;
using BackPredictFinance.Datas.Entities;
using Microsoft.EntityFrameworkCore;

namespace BackPredictFinance.API.SeedData
{
    internal sealed class EducationArticlesSeedService(
        IServiceScopeFactory scopeFactory,
        IHostEnvironment env,
        ILogger<EducationArticlesSeedService> logger)
        : JsonContentSeeder<EducationArticle, EducationArticlesSeedService.EducationArticleSeedRecord>(scopeFactory, env, logger)
    {
        protected override string FileName => "education-articles.json";
        protected override string EntityLabel => "article(s) éducatif(s)";

        protected override IQueryable<string> ExistingKeys(DbSet<EducationArticle> entities)
            => entities.IgnoreQueryFilters().Select(a => a.Slug);

        protected override string RecordKey(EducationArticleSeedRecord record) => record.Slug;

        protected override EducationArticle Map(EducationArticleSeedRecord record) => new()
        {
            Slug = record.Slug,
            ProductType = Enum.Parse<EducationProductTypeEnum>(record.ProductType),
            Title = record.Title,
            Summary = record.Summary,
            BodyMarkdown = record.BodyMarkdown,
            DisplayOrder = record.DisplayOrder,
            IsActive = true,
            IsPublished = true
        };

        internal sealed class EducationArticleSeedRecord
        {
            public string Slug { get; set; } = string.Empty;
            public string ProductType { get; set; } = string.Empty;
            public string Title { get; set; } = string.Empty;
            public string Summary { get; set; } = string.Empty;
            public string BodyMarkdown { get; set; } = string.Empty;
            public int DisplayOrder { get; set; }
        }
    }
}
