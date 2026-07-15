using System.Net.Http;
using System.Text.Json;
using BackPredictFinance.Services.TwelveDataServices;
using BackPredictFinance.ViewModels.ClientFinanceViewModels.Fundamentals;
using Microsoft.Extensions.Logging;

namespace BackPredictFinance.Services.ClientFinanceServices.Fundamentals
{
    public interface IClientFinanceFundamentalsService
    {
        Task<InstrumentFundamentalsViewModel?> GetFundamentalsAsync(string symbol, CancellationToken ct = default);
    }

    public sealed class ClientFinanceFundamentalsService : IClientFinanceFundamentalsService
    {
        private readonly IFundamentalsProvider _fundamentalsProvider;
        private readonly ILogger<ClientFinanceFundamentalsService> _logger;

        public ClientFinanceFundamentalsService(
            IFundamentalsProvider fundamentalsProvider,
            ILogger<ClientFinanceFundamentalsService> logger)
        {
            _fundamentalsProvider = fundamentalsProvider;
            _logger = logger;
        }

        public async Task<InstrumentFundamentalsViewModel?> GetFundamentalsAsync(string symbol, CancellationToken ct = default)
        {
            var normalized = symbol.Trim().ToUpperInvariant();

            try
            {
                var data = await _fundamentalsProvider.GetFundamentalsAsync(normalized, ct);

                if (data is null)
                    return null;

                return new InstrumentFundamentalsViewModel
                {
                    Symbol = data.Symbol,
                    CompanyName = data.CompanyName,
                    Sector = data.Sector,
                    Currency = data.Currency,
                    AsOfUtc = data.AsOfUtc,
                    TrailingPe = data.TrailingPe,
                    DividendYield = data.DividendYield.HasValue ? Math.Round(data.DividendYield.Value * 100, 4) : null,
                    ReturnOnEquity = data.ReturnOnEquity.HasValue ? Math.Round(data.ReturnOnEquity.Value * 100, 4) : null,
                    OperatingMargin = data.OperatingMargin.HasValue ? Math.Round(data.OperatingMargin.Value * 100, 4) : null,
                    CurrentRatio = data.CurrentRatio,
                    DebtToEquity = data.DebtToEquity,
                    RevenueGrowth = data.RevenueGrowth.HasValue ? Math.Round(data.RevenueGrowth.Value * 100, 4) : null,
                    EarningsGrowth = data.EarningsGrowth.HasValue ? Math.Round(data.EarningsGrowth.Value * 100, 4) : null,
                    PegRatio = data.PegRatio,
                    PriceToBook = data.PriceToBook,
                    RecommendationKey = data.RecommendationKey,
                    RecommendationMean = data.RecommendationMean,
                    TargetMeanPrice = data.TargetMeanPrice
                };
            }
            catch (OperationCanceledException ex)
            {
                _logger.LogWarning(ex, "Fundamentals cancelled for symbol {Symbol}", normalized);
                return null;
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Fundamentals provider error for symbol {Symbol}", normalized);
                return null;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogWarning(ex, "Fundamentals HTTP error for symbol {Symbol}", normalized);
                return null;
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Fundamentals JSON parsing error for symbol {Symbol}", normalized);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fundamentals unexpected error for symbol {Symbol}", normalized);
                return null;
            }
        }
    }
}
