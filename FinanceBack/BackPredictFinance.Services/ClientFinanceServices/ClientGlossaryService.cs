using BackPredictFinance.ViewModels.ClientFinanceViewModels.Readings;
using Microsoft.EntityFrameworkCore;

namespace BackPredictFinance.Services.ClientFinanceServices
{
    public interface IClientGlossaryService
    {
        Task<List<GlossaryTermViewModel>> GetGlossaryAsync(CancellationToken ct = default);
    }

    public sealed class ClientGlossaryService : BaseService, IClientGlossaryService
    {
        public ClientGlossaryService(IServiceProvider serviceProvider)
            : base(serviceProvider)
        {
        }

        public async Task<List<GlossaryTermViewModel>> GetGlossaryAsync(CancellationToken ct = default)
        {
            return await _financeDbContext.ParameterDictionaryEntries
                .AsNoTracking()
                .Where(e => e.IsActive && e.IsPublished)
                .OrderBy(e => e.DisplayLabel)
                .Select(e => new GlossaryTermViewModel
                {
                    ParameterId = e.ParameterId,
                    Label = e.DisplayLabel,
                    Definition = e.SimpleDefinition
                })
                .ToListAsync(ct);
        }
    }
}
