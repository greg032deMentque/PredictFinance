using BackPredictFinance.ViewModels.AdminViewModels.ParameterDictionary;
using Microsoft.EntityFrameworkCore;

namespace BackPredictFinance.Services.AdminGovernance
{
    public interface IAdminParameterDictionaryAdminService
    {
        Task<List<AdminParameterDictionaryItemViewModel>> GetListAsync(CancellationToken ct = default);
        Task<AdminParameterDictionaryDetailViewModel> GetDetailAsync(string parameterId, CancellationToken ct = default);
    }

    public sealed class AdminParameterDictionaryAdminService : BaseService, IAdminParameterDictionaryAdminService
    {
        public AdminParameterDictionaryAdminService(IServiceProvider serviceProvider) : base(serviceProvider)
        {
        }

        public async Task<List<AdminParameterDictionaryItemViewModel>> GetListAsync(CancellationToken ct = default)
        {
            return await _financeDbContext.ParameterDictionaryEntries
                .AsNoTracking()
                .OrderBy(x => x.CategoryCode)
                .ThenBy(x => x.ParameterId)
                .Select(x => new AdminParameterDictionaryItemViewModel
                {
                    ParameterId = x.ParameterId,
                    CategoryCode = x.CategoryCode,
                    DisplayLabel = x.DisplayLabel,
                    IsActive = x.IsActive,
                    IsPublished = x.IsPublished
                })
                .ToListAsync(ct);
        }

        public async Task<AdminParameterDictionaryDetailViewModel> GetDetailAsync(string parameterId, CancellationToken ct = default)
        {
            var normalizedParameterId = (parameterId ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(normalizedParameterId))
            {
                throw new ArgumentException("Parameter id is required.", nameof(parameterId));
            }

            var detail = await _financeDbContext.ParameterDictionaryEntries
                .AsNoTracking()
                .Where(x => x.ParameterId == normalizedParameterId)
                .Select(x => new AdminParameterDictionaryDetailViewModel
                {
                    ParameterId = x.ParameterId,
                    CategoryCode = x.CategoryCode,
                    DisplayLabel = x.DisplayLabel,
                    RoleInCategory = x.RoleInCategory,
                    SimpleDefinition = x.SimpleDefinition,
                    HowToRead = x.HowToRead,
                    WhyItMatters = x.WhyItMatters,
                    LimitsOfInterpretation = x.LimitsOfInterpretation,
                    WhatItSupports = x.WhatItSupports,
                    WhatItDoesNotProve = x.WhatItDoesNotProve,
                    ImplicationWithoutPosition = x.ImplicationWithoutPosition,
                    ImplicationWithPosition = x.ImplicationWithPosition,
                    IsActive = x.IsActive,
                    IsPublished = x.IsPublished
                })
                .FirstOrDefaultAsync(ct);

            return detail ?? throw new KeyNotFoundException($"Parameter '{normalizedParameterId}' was not found.");
        }
    }
}
