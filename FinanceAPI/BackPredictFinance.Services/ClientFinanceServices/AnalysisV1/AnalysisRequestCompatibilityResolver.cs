using BackPredictFinance.Datas.Context;
using BackPredictFinance.ViewModels.ClientFinanceViewModels;
using Microsoft.EntityFrameworkCore;

namespace BackPredictFinance.Services.ClientFinanceServices.AnalysisV1
{
    public sealed class AnalysisRequestCompatibilityResolver : IAnalysisRequestCompatibilityResolver
    {
        private readonly FinanceDbContext _financeDbContext;

        public AnalysisRequestCompatibilityResolver(FinanceDbContext financeDbContext)
        {
            _financeDbContext = financeDbContext;
        }

        public async Task<ResolvedAnalysisRunRequest> ResolveAsync(AnalysisRunRequestViewModel request, string userId, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(request);

            var symbol = NormalizeSymbol(request.Symbol);
            if (string.IsNullOrWhiteSpace(symbol))
            {
                throw new ArgumentException("Le symbole est obligatoire.", nameof(request.Symbol));
            }

            var requestedPatternId = NormalizePatternId(request.RequestedPattern);

            var holdsInstrument = await _financeDbContext.UserAssets
                .AsNoTracking()
                .Include(userAsset => userAsset.Asset)
                .AnyAsync(
                    userAsset => userAsset.UserId == userId &&
                                 userAsset.Quantity > 0m &&
                                 userAsset.Asset.Symbol == symbol,
                    ct);

            return new ResolvedAnalysisRunRequest
            {
                UserId = userId,
                Symbol = symbol,
                RequestedPatternId = requestedPatternId,
                HoldsInstrument = holdsInstrument
            };
        }

        private static string NormalizeSymbol(string? symbol)
        {
            return (symbol ?? string.Empty).Trim().ToUpperInvariant();
        }

        private static string NormalizePatternId(string? requestedPattern)
        {
            return (requestedPattern ?? string.Empty).Trim().ToUpperInvariant();
        }
    }
}
