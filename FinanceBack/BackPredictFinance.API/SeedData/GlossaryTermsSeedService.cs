using BackPredictFinance.Common.enums;
using BackPredictFinance.Datas.Context;
using BackPredictFinance.Datas.Entities;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace BackPredictFinance.API.SeedData
{
    public sealed class GlossaryTermsSeedService(
        IServiceScopeFactory scopeFactory,
        IHostEnvironment env,
        ILogger<GlossaryTermsSeedService> logger) : IHostedService
    {
        private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            var path = Path.Combine(env.ContentRootPath, "SeedData", "glossary-terms.json");
            if (!File.Exists(path))
            {
                logger.LogWarning("glossary-terms.json introuvable à {Path}", path);
                return;
            }

            await using var stream = File.OpenRead(path);
            var records = await JsonSerializer.DeserializeAsync<List<GlossaryTermSeedRecord>>(stream, JsonOptions, cancellationToken);

            if (records is null or { Count: 0 }) return;

            await using var scope = scopeFactory.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<FinanceDbContext>();

            var existingKeys = await context.GlossaryTerms
                .IgnoreQueryFilters()
                .Select(t => t.NormalizedTerm)
                .ToHashSetAsync(cancellationToken);

            var toInsert = records
                .Where(r => !existingKeys.Contains(r.NormalizedTerm))
                .Select(r => new GlossaryTerm
                {
                    Term = r.Term,
                    NormalizedTerm = r.NormalizedTerm,
                    Definition = r.Definition,
                    Category = Enum.Parse<GlossaryTermEnum>(r.Category),
                    IsActive = true,
                    IsPublished = true
                })
                .ToList();

            if (toInsert.Count == 0) return;

            context.GlossaryTerms.AddRange(toInsert);
            await context.SaveChangesAsync(cancellationToken);

            logger.LogInformation("{Count} terme(s) de glossaire inséré(s) depuis glossary-terms.json", toInsert.Count);
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        private sealed class GlossaryTermSeedRecord
        {
            public string Term { get; set; } = string.Empty;
            public string NormalizedTerm { get; set; } = string.Empty;
            public string Definition { get; set; } = string.Empty;
            public string Category { get; set; } = string.Empty;
        }
    }
}
