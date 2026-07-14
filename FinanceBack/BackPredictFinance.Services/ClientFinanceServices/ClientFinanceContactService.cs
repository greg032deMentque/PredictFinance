using BackPredictFinance.Common.Email;
using BackPredictFinance.Services.AuthServices;
using BackPredictFinance.ViewModels.ClientFinanceViewModels.Contact;
using Microsoft.Extensions.Options;

namespace BackPredictFinance.Services.ClientFinanceServices
{
    public interface IClientFinanceContactService
    {
        Task SendSupportMessageAsync(ContactSupportRequestViewModel request, CancellationToken ct = default);
    }

    public sealed class ClientFinanceContactService : BaseService, IClientFinanceContactService
    {
        private const int MaxSubjectLength = 160;
        private const int MaxMessageLength = 4000;

        private readonly ICurrentUserSessionService _currentUserSessionService;
        private readonly IEmailService _emailService;
        private readonly EmailServiceConfiguration _emailConfiguration;

        public ClientFinanceContactService(
            IServiceProvider serviceProvider,
            ICurrentUserSessionService currentUserSessionService,
            IEmailService emailService,
            IOptions<EmailServiceConfiguration> emailOptions)
            : base(serviceProvider)
        {
            _currentUserSessionService = currentUserSessionService;
            _emailService = emailService;
            _emailConfiguration = emailOptions.Value;
        }

        public async Task SendSupportMessageAsync(ContactSupportRequestViewModel request, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(request);

            var subject = NormalizeRequiredText(request.Subject, nameof(request.Subject), MaxSubjectLength);
            var message = NormalizeRequiredText(request.Message, nameof(request.Message), MaxMessageLength);
            var currentUser = await _currentUserSessionService.GetCurrentAsync(ct)
                ?? throw new InvalidOperationException("Aucun utilisateur authentifie n'est disponible pour ce contact.");

            var recipients = _emailConfiguration.To
                .Where(address => !string.IsNullOrWhiteSpace(address))
                .Select(address => address.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (recipients.Count == 0)
            {
                throw new InvalidOperationException("Aucun destinataire de contact n'est configure.");
            }

            var emailBody = string.Join(Environment.NewLine, new[]
            {
                "Nouveau message depuis l'onglet Contact PredictFinance.",
                $"Date UTC : {DateTime.UtcNow:O}",
                $"Utilisateur : {currentUser.DisplayName}",
                $"Email : {currentUser.Email}",
                $"UserId : {currentUser.UserId}",
                $"Sujet : {subject}",
                string.Empty,
                "Message :",
                message
            });

            foreach (var recipient in recipients)
            {
                await _emailService.SendEmail(
                    recipient,
                    $"[PredictFinance Contact] {subject}",
                    emailBody,
                    isBodyHtml: false);
            }
        }

        private static string NormalizeRequiredText(string? rawValue, string parameterName, int maxLength)
        {
            var normalizedValue = (rawValue ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(normalizedValue))
            {
                throw new ArgumentException("Le champ est obligatoire.", parameterName);
            }

            if (normalizedValue.Length > maxLength)
            {
                throw new ArgumentException($"Le champ ne peut pas depasser {maxLength} caracteres.", parameterName);
            }

            return normalizedValue;
        }
    }
}
