using BackPredictFinance.Common.enums;
using BackPredictFinance.Datas.Entities;
using Microsoft.EntityFrameworkCore;

namespace BackPredictFinance.API.SeedData
{
    internal sealed class GlossaryTermsSeedService(
        IServiceScopeFactory scopeFactory,
        IHostEnvironment env,
        ILogger<GlossaryTermsSeedService> logger)
        : JsonContentSeeder<GlossaryTerm, GlossaryTermsSeedService.GlossaryTermSeedRecord>(scopeFactory, env, logger)
    {
        protected override string FileName => "glossary-terms.json";
        protected override string EntityLabel => "terme(s) de glossaire";

        protected override IQueryable<string> ExistingKeys(DbSet<GlossaryTerm> entities)
            => entities.IgnoreQueryFilters().Select(t => t.NormalizedTerm);

        protected override string RecordKey(GlossaryTermSeedRecord record) => record.NormalizedTerm;

        protected override GlossaryTerm Map(GlossaryTermSeedRecord record) => new()
        {
            Term = record.Term,
            NormalizedTerm = record.NormalizedTerm,
            Definition = record.Definition,
            Category = Enum.Parse<GlossaryTermEnum>(record.Category),
            IsActive = true,
            IsPublished = true
        };

        internal sealed class GlossaryTermSeedRecord
        {
            public string Term { get; set; } = string.Empty;
            public string NormalizedTerm { get; set; } = string.Empty;
            public string Definition { get; set; } = string.Empty;
            public string Category { get; set; } = string.Empty;
        }
    }
}
