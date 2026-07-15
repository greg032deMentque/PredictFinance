using BackPredictFinance.Datas.Context;
using BackPredictFinance.Datas.Entities;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace BackPredictFinance.API.SeedData
{
    public sealed class ConceptsSeedService(
        IServiceScopeFactory scopeFactory,
        IHostEnvironment env,
        ILogger<ConceptsSeedService> logger) : IHostedService
    {
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            var path = Path.Combine(env.ContentRootPath, "SeedData", "analysis-concepts.json");
            if (!File.Exists(path))
            {
                logger.LogWarning("analysis-concepts.json introuvable à {Path}", path);
                return;
            }

            await using var stream = File.OpenRead(path);
            var concepts = await JsonSerializer.DeserializeAsync<List<AnalysisConceptExplanation>>(
                stream,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true },
                cancellationToken);

            if (concepts is null or { Count: 0 }) return;

            await using var scope = scopeFactory.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<FinanceDbContext>();

            var existingCodes = await context.AnalysisConceptExplanations
                .Select(c => c.Code)
                .ToHashSetAsync(cancellationToken);

            var toInsert = concepts.Where(c => !existingCodes.Contains(c.Code)).ToList();
            if (toInsert.Count == 0) return;

            context.AnalysisConceptExplanations.AddRange(toInsert);
            await context.SaveChangesAsync(cancellationToken);

            logger.LogInformation("{Count} concept(s) inséré(s) depuis analysis-concepts.json", toInsert.Count);
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
