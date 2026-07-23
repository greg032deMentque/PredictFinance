using BackPredictFinance.Datas.Entities;
using Microsoft.EntityFrameworkCore;

namespace BackPredictFinance.API.SeedData
{
    internal sealed class ConceptsSeedService(
        IServiceScopeFactory scopeFactory,
        IHostEnvironment env,
        ILogger<ConceptsSeedService> logger)
        : JsonContentSeeder<AnalysisConceptExplanation, AnalysisConceptExplanation>(scopeFactory, env, logger)
    {
        protected override string FileName => "analysis-concepts.json";
        protected override string EntityLabel => "concept(s)";

        protected override IQueryable<string> ExistingKeys(DbSet<AnalysisConceptExplanation> entities)
            => entities.Select(c => c.Code);

        protected override string RecordKey(AnalysisConceptExplanation record) => record.Code;

        protected override AnalysisConceptExplanation Map(AnalysisConceptExplanation record) => record;
    }
}
