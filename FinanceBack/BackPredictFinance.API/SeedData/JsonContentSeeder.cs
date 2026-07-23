using BackPredictFinance.Datas.Context;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace BackPredictFinance.API.SeedData
{
    internal static class JsonContentSeederDefaults
    {
        internal static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };
    }

    internal abstract class JsonContentSeeder<TEntity, TSeedRecord>(
        IServiceScopeFactory scopeFactory,
        IHostEnvironment env,
        ILogger logger) : IHostedService
        where TEntity : class
    {
        protected abstract string FileName { get; }
        protected abstract string EntityLabel { get; }

        protected abstract IQueryable<string> ExistingKeys(DbSet<TEntity> entities);
        protected abstract string RecordKey(TSeedRecord record);
        protected abstract TEntity Map(TSeedRecord record);

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            var path = Path.Combine(env.ContentRootPath, "SeedData", FileName);
            if (!File.Exists(path))
            {
                logger.LogWarning("{FileName} introuvable à {Path}", FileName, path);
                return;
            }

            await using var stream = File.OpenRead(path);
            var records = await JsonSerializer.DeserializeAsync<List<TSeedRecord>>(stream, JsonContentSeederDefaults.JsonOptions, cancellationToken);

            if (records is null or { Count: 0 }) return;

            await using var scope = scopeFactory.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<FinanceDbContext>();
            var dbSet = context.Set<TEntity>();

            var existingKeys = await ExistingKeys(dbSet).ToHashSetAsync(cancellationToken);

            var toInsert = records
                .Where(r => !existingKeys.Contains(RecordKey(r)))
                .Select(Map)
                .ToList();

            if (toInsert.Count == 0) return;

            dbSet.AddRange(toInsert);
            await context.SaveChangesAsync(cancellationToken);

            logger.LogInformation("{Count} {EntityLabel} inséré(s) depuis {FileName}", toInsert.Count, EntityLabel, FileName);
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
