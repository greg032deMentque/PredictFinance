using BackPredictFinance.Common.enums;
using BackPredictFinance.ViewModels.ClientFinanceViewModels.Readings;
using Microsoft.EntityFrameworkCore;

namespace BackPredictFinance.Services.ClientFinanceServices
{
    public interface IClientFinanceParameterDetailService
    {
        Task<ParameterDetailViewModel?> GetParameterDetailAsync(string analysisId, string parameterId, CancellationToken ct = default);
    }

    public sealed class ClientFinanceParameterDetailService : BaseService, IClientFinanceParameterDetailService
    {
        private readonly IClientFinanceProjectionService _projectionService;

        public ClientFinanceParameterDetailService(
            IServiceProvider serviceProvider,
            IClientFinanceProjectionService projectionService)
            : base(serviceProvider)
        {
            _projectionService = projectionService;
        }

        public async Task<ParameterDetailViewModel?> GetParameterDetailAsync(
            string analysisId,
            string parameterId,
            CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(analysisId) || string.IsNullOrWhiteSpace(parameterId))
            {
                return null;
            }

            var userId = _currentUserId;
            if (string.IsNullOrWhiteSpace(userId))
            {
                return null;
            }

            var entry = await _financeDbContext.ParameterDictionaryEntries
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.ParameterId == parameterId && x.IsActive && x.IsPublished, ct);

            if (entry is null)
            {
                return null;
            }

            var run = await _financeDbContext.AnalysisRuns
                .AsNoTracking()
                .Include(x => x.Asset)
                .FirstOrDefaultAsync(x => x.Id == analysisId && x.UserId == userId, ct);

            if (run is null)
            {
                return null;
            }

            var userAsset = await _financeDbContext.UserAssets
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.UserId == userId && x.AssetId == run.AssetId, ct);

            var holdsInstrument = userAsset is not null && userAsset.Quantity > 0m;
            var holdingStatus = holdsInstrument ? HoldingStatusEnum.Held : HoldingStatusEnum.NotHeld;
            var holdingContextLabel = holdsInstrument ? "Vous détenez cet instrument." : "Vous ne détenez pas cet instrument.";

            var latestPeaEligibility = await _projectionService.GetLatestPeaEligibilityAsync(run.AssetId, ct);
            var peaStatus = latestPeaEligibility?.EligibilityStatus ?? PeaEligibilityStatusEnum.Unknown;
            var peaLabel = BuildPeaDisplayLabel(peaStatus);

            return new ParameterDetailViewModel
            {
                ParameterId = entry.ParameterId,
                CategoryCode = entry.CategoryCode,
                Label = entry.DisplayLabel,
                RoleInCategory = entry.RoleInCategory,
                SimpleDefinition = entry.SimpleDefinition,
                HowToReadCurrentValue = entry.HowToRead,
                WhyItMatters = entry.WhyItMatters,
                LimitsOfInterpretation = entry.LimitsOfInterpretation,
                WhatItSupports = entry.WhatItSupports,
                WhatItDoesNotProve = entry.WhatItDoesNotProve,
                ImplicationWithoutPosition = entry.ImplicationWithoutPosition,
                ImplicationWithPosition = entry.ImplicationWithPosition,
                Instrument = _projectionService.BuildInstrumentIdentity(run.Asset),
                HoldingStatus = holdingStatus,
                HoldingContextLabel = holdingContextLabel,
                CurrentValue = new ParameterCurrentValueViewModel
                {
                    IsAvailable = false,
                    AvailabilityLabel = "Valeur non disponible pour ce paramètre dans cette version."
                },
                PeaEligibilityStatus = peaStatus,
                PeaDisplayLabel = peaLabel
            };
        }

        private static string BuildPeaDisplayLabel(PeaEligibilityStatusEnum status)
        {
            return status switch
            {
                PeaEligibilityStatusEnum.ConfirmedEligible => "Éligible PEA",
                PeaEligibilityStatusEnum.ConfirmedIneligible => "Non éligible PEA",
                _ => "Éligibilité PEA inconnue"
            };
        }
    }
}
