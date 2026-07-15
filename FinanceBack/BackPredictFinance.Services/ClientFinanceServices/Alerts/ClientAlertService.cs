using BackPredictFinance.Common.enums;
using BackPredictFinance.Services.Notifications;
using BackPredictFinance.ViewModels.ClientFinanceViewModels.Alerts;
using BackPredictFinance.ViewModels.NotificationViewModels;
using Microsoft.EntityFrameworkCore;

namespace BackPredictFinance.Services.ClientFinanceServices.Alerts
{
    public interface IClientAlertService
    {
        Task<NotificationItemViewModel> CreateAlertAsync(CreateClientAlertRequestViewModel request, CancellationToken ct = default);
    }

    public sealed class ClientAlertService : BaseService, IClientAlertService
    {
        private readonly IClientFinanceAssetSupportService _assetSupportService;
        private readonly IProactiveAlertEmitter _alertEmitter;

        public ClientAlertService(
            IServiceProvider serviceProvider,
            IClientFinanceAssetSupportService assetSupportService,
            IProactiveAlertEmitter alertEmitter)
            : base(serviceProvider)
        {
            _assetSupportService = assetSupportService;
            _alertEmitter = alertEmitter;
        }

        public async Task<NotificationItemViewModel> CreateAlertAsync(CreateClientAlertRequestViewModel request, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(request);

            var symbol = _assetSupportService.NormalizeSymbol(request.Symbol);
            if (string.IsNullOrWhiteSpace(symbol))
            {
                throw new ArgumentException("Le symbole est obligatoire.", nameof(request));
            }

            var userId = _assetSupportService.GetRequiredCurrentUserId();
            var asset = await _financeDbContext.Assets
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Symbol == symbol, ct)
                ?? throw new KeyNotFoundException($"Aucun actif trouve pour le symbole {symbol}.");

            var title = BuildTitle(symbol, request.Trigger, request.LevelValue);
            var summary = BuildSummary(symbol, request.Trigger, request.LevelValue, request.PatternId);

            await _alertEmitter.EmitAsync(
                _financeDbContext,
                userId,
                request.Trigger,
                NotificationTargetScreenEnum.InstrumentDetail,
                asset.Id,
                DateTime.UtcNow.Date,
                title,
                summary,
                ct);

            var notification = await _financeDbContext.UserNotifications
                .AsNoTracking()
                .Where(x => x.UserId == userId
                    && x.TargetEntityId == asset.Id
                    && x.AlertTrigger == request.Trigger
                    && x.AlertDayKeyUtc == DateTime.UtcNow.Date)
                .OrderByDescending(x => x.CreatedAtUtc)
                .Select(x => new NotificationItemViewModel
                {
                    NotificationId = x.NotificationId,
                    UserId = x.UserId,
                    Category = x.Category,
                    Status = x.Status,
                    Title = x.Title,
                    Summary = x.Summary,
                    CreatedAtUtc = x.CreatedAtUtc,
                    TargetScreen = x.TargetScreen,
                    TargetEntityId = x.TargetEntityId,
                    ReadAtUtc = x.ReadAtUtc,
                    AlertTrigger = x.AlertTrigger
                })
                .FirstOrDefaultAsync(ct);

            if (notification == null)
            {
                return new NotificationItemViewModel
                {
                    NotificationId = string.Empty,
                    UserId = userId,
                    Category = NotificationCategoryEnum.Analysis,
                    Status = NotificationStatusEnum.Unread,
                    Title = title,
                    Summary = summary,
                    CreatedAtUtc = DateTime.UtcNow,
                    TargetScreen = NotificationTargetScreenEnum.InstrumentDetail,
                    TargetEntityId = asset.Id,
                    AlertTrigger = request.Trigger
                };
            }

            return notification;
        }

        private static string BuildTitle(string symbol, AlertTrigger trigger, decimal? levelValue)
        {
            return trigger switch
            {
                AlertTrigger.PatternStateChange => $"Alerte pattern — {symbol}",
                AlertTrigger.LevelCrossed => levelValue.HasValue
                    ? $"Alerte niveau {levelValue.Value:0.####} — {symbol}"
                    : $"Alerte niveau — {symbol}",
                _ => $"Alerte donnees obsoletes — {symbol}"
            };
        }

        private static string BuildSummary(string symbol, AlertTrigger trigger, decimal? levelValue, string? patternId)
        {
            return trigger switch
            {
                AlertTrigger.PatternStateChange => string.IsNullOrWhiteSpace(patternId)
                    ? $"Vous serez notifie en cas de changement d etat du pattern sur {symbol}."
                    : $"Vous serez notifie en cas de changement d etat du pattern {patternId} sur {symbol}.",
                AlertTrigger.LevelCrossed => levelValue.HasValue
                    ? $"Vous serez notifie si {symbol} franchit le niveau {levelValue.Value:0.####}."
                    : $"Vous serez notifie si {symbol} franchit le niveau de reference.",
                _ => $"Vous serez notifie si les donnees de {symbol} deviennent obsoletes."
            };
        }
    }
}
