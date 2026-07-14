using BackPredictFinance.Common.MarketData;
using BackPredictFinance.Services.TwelveDataServices;
using BackPredictFinance.ViewModels.ClientFinanceViewModels.Instruments;

namespace BackPredictFinance.Services.Fundamentals
{
    public interface IEtfReadingService
    {
        Task<EtfReadingViewModel> GetEtfReadingAsync(string symbol, CancellationToken ct = default);
    }

    public sealed class EtfReadingService : BaseService, IEtfReadingService
    {
        private readonly IEtfProfileProvider _etfProfileProvider;

        public EtfReadingService(IServiceProvider serviceProvider, IEtfProfileProvider etfProfileProvider)
            : base(serviceProvider)
        {
            _etfProfileProvider = etfProfileProvider;
        }

        public async Task<EtfReadingViewModel> GetEtfReadingAsync(string symbol, CancellationToken ct = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(symbol);

            var normalized = symbol.Trim().ToUpperInvariant();
            MarketEtfProfileData profile;

            try
            {
                profile = await _etfProfileProvider.GetEtfProfileAsync(normalized, ct);
            }
            catch (Exception ex) when (ex is InvalidOperationException or HttpRequestException or System.Text.Json.JsonException)
            {
                profile = new MarketEtfProfileData { Symbol = normalized };
            }

            return _mapper.Map<EtfReadingViewModel>(profile);
        }
    }
}
