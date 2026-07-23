using BackPredictFinance.Common.enums;
using BackPredictFinance.Datas.Context;
using BackPredictFinance.Datas.Entities;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace BackPredictFinance.API.SeedData
{
    public sealed class EducationArticlesSeedService(
        IServiceScopeFactory scopeFactory,
        IHostEnvironment env,
        ILogger<EducationArticlesSeedService> logger) : IHostedService
    {
        private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            var path = Path.Combine(env.ContentRootPath, "SeedData", "education-articles.json");
            if (!File.Exists(path))
            {
                logger.LogWarning("education-articles.json introuvable à {Path}", path);
                return;
            }

            await using var stream = File.OpenRead(path);
            var records = await JsonSerializer.DeserializeAsync<List<EducationArticleSeedRecord>>(stream, JsonOptions, cancellationToken);

            if (records is null or { Count: 0 }) return;

            await using var scope = scopeFactory.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<FinanceDbContext>();

            var existingSlugs = await context.EducationArticles
                .IgnoreQueryFilters()
                .Select(a => a.Slug)
                .ToHashSetAsync(cancellationToken);

            var toInsert = records
                .Where(r => !existingSlugs.Contains(r.Slug))
                .Select(r => new EducationArticle
                {
                    Slug = r.Slug,
                    ProductType = Enum.Parse<EducationProductTypeEnum>(r.ProductType),
                    Title = r.Title,
                    Summary = r.Summary,
                    BodyMarkdown = r.BodyMarkdown,
                    DisplayOrder = r.DisplayOrder,
                    IsActive = true,
                    IsPublished = true
                })
                .ToList();

            if (toInsert.Count == 0) return;

            context.EducationArticles.AddRange(toInsert);
            await context.SaveChangesAsync(cancellationToken);

            logger.LogInformation("{Count} article(s) éducatif(s) inséré(s) depuis education-articles.json", toInsert.Count);
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        private sealed class EducationArticleSeedRecord
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
