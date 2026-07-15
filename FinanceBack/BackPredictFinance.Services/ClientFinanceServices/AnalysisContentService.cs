using BackPredictFinance.Datas.Entities;
using BackPredictFinance.ViewModels.AdminViewModels.Content;
using Microsoft.EntityFrameworkCore;

namespace BackPredictFinance.Services.ClientFinanceServices
{
    public interface IAnalysisContentService
    {
        Task<List<PatternDefinitionAdminViewModel>> GetPatternsAsync(CancellationToken ct = default);
        Task<PatternDefinitionAdminViewModel?> GetPatternByIdAsync(string patternId, CancellationToken ct = default);
        Task<PatternDefinitionAdminViewModel> UpdatePatternAsync(string patternId, PatternDefinitionUpdateRequestViewModel request, CancellationToken ct = default);

        Task<List<AnalysisConceptAdminViewModel>> GetConceptsAsync(CancellationToken ct = default);
        Task<AnalysisConceptAdminViewModel?> GetConceptByCodeAsync(string code, CancellationToken ct = default);
        Task<AnalysisConceptAdminViewModel> CreateConceptAsync(AnalysisConceptCreateRequestViewModel request, CancellationToken ct = default);
        Task<AnalysisConceptAdminViewModel> UpdateConceptAsync(string code, AnalysisConceptUpdateRequestViewModel request, CancellationToken ct = default);
        Task DeleteConceptAsync(string code, CancellationToken ct = default);
    }

    public sealed class AnalysisContentService : BaseService, IAnalysisContentService
    {
        public AnalysisContentService(IServiceProvider serviceProvider) : base(serviceProvider) { }

        public async Task<List<PatternDefinitionAdminViewModel>> GetPatternsAsync(CancellationToken ct = default)
        {
            var entries = await _financeDbContext.PatternDefinitions
                .AsNoTracking()
                .OrderBy(p => p.Family)
                .ThenBy(p => p.DisplayName)
                .ToListAsync(ct);

            return _mapper.Map<List<PatternDefinitionAdminViewModel>>(entries);
        }

        public async Task<PatternDefinitionAdminViewModel?> GetPatternByIdAsync(string patternId, CancellationToken ct = default)
        {
            var entry = await _financeDbContext.PatternDefinitions
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.PatternId == patternId, ct);

            return entry is null ? null : _mapper.Map<PatternDefinitionAdminViewModel>(entry);
        }

        public async Task<PatternDefinitionAdminViewModel> UpdatePatternAsync(string patternId, PatternDefinitionUpdateRequestViewModel request, CancellationToken ct = default)
        {
            var entry = await _financeDbContext.PatternDefinitions
                .FirstOrDefaultAsync(p => p.PatternId == patternId, ct);

            if (entry is null)
                throw new KeyNotFoundException($"Pattern {patternId} introuvable.");

            entry.DisplayName = request.DisplayName;
            entry.Family = request.Family;
            entry.FamilyLabel = request.FamilyLabel;
            entry.Direction = request.Direction;
            entry.DirectionLabel = request.DirectionLabel;
            entry.Description = request.Description;
            entry.AnalysisNarrative = request.AnalysisNarrative;
            entry.Reliability = request.Reliability;
            entry.ReliabilityLabel = request.ReliabilityLabel;

            await _financeDbContext.SaveChangesAsync(ct);

            return _mapper.Map<PatternDefinitionAdminViewModel>(entry);
        }

        public async Task<List<AnalysisConceptAdminViewModel>> GetConceptsAsync(CancellationToken ct = default)
        {
            var entries = await _financeDbContext.AnalysisConceptExplanations
                .AsNoTracking()
                .OrderBy(c => c.Code)
                .ToListAsync(ct);

            return _mapper.Map<List<AnalysisConceptAdminViewModel>>(entries);
        }

        public async Task<AnalysisConceptAdminViewModel?> GetConceptByCodeAsync(string code, CancellationToken ct = default)
        {
            var entry = await _financeDbContext.AnalysisConceptExplanations
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Code == code, ct);

            return entry is null ? null : _mapper.Map<AnalysisConceptAdminViewModel>(entry);
        }

        public async Task<AnalysisConceptAdminViewModel> CreateConceptAsync(AnalysisConceptCreateRequestViewModel request, CancellationToken ct = default)
        {
            var exists = await _financeDbContext.AnalysisConceptExplanations
                .AnyAsync(c => c.Code == request.Code, ct);

            if (exists)
                throw new InvalidOperationException($"Le concept avec le code « {request.Code} » existe déjà.");

            var entry = new AnalysisConceptExplanation
            {
                Code = request.Code.Trim().ToUpperInvariant(),
                Label = request.Label,
                Explanation = request.Explanation
            };

            _financeDbContext.AnalysisConceptExplanations.Add(entry);
            await _financeDbContext.SaveChangesAsync(ct);

            return _mapper.Map<AnalysisConceptAdminViewModel>(entry);
        }

        public async Task<AnalysisConceptAdminViewModel> UpdateConceptAsync(string code, AnalysisConceptUpdateRequestViewModel request, CancellationToken ct = default)
        {
            var entry = await _financeDbContext.AnalysisConceptExplanations
                .FirstOrDefaultAsync(c => c.Code == code, ct);

            if (entry is null)
                throw new KeyNotFoundException($"Concept {code} introuvable.");

            entry.Label = request.Label;
            entry.Explanation = request.Explanation;

            await _financeDbContext.SaveChangesAsync(ct);

            return _mapper.Map<AnalysisConceptAdminViewModel>(entry);
        }

        public async Task DeleteConceptAsync(string code, CancellationToken ct = default)
        {
            var entry = await _financeDbContext.AnalysisConceptExplanations
                .FirstOrDefaultAsync(c => c.Code == code, ct);

            if (entry is null)
                throw new KeyNotFoundException($"Concept {code} introuvable.");

            _financeDbContext.AnalysisConceptExplanations.Remove(entry);
            await _financeDbContext.SaveChangesAsync(ct);
        }
    }
}
