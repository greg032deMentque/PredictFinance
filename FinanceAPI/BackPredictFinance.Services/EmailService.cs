using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Options;
using MimeKit;
using BackPredictFinance.Common.Email;
using BackPredictFinance.Common;

namespace BackPredictFinance.Services
{
    public interface IEmailService
    {
        Task SendEmailPasswordReset(string userEmail, string link);
        Task SendEmail(
            string userEmail,
            string subject,
            string body,
            bool isBodyHtml = true,
            List<EmailAttachment>? attachments = null);

        EmailAttachment CreateAttachmentFromPath(string filePath);
    }

    public sealed class EmailService : BaseService, IEmailService
    {
        private readonly EmailServiceConfiguration _config;
        private static readonly FileExtensionContentTypeProvider ContentTypeProvider = new();

        public EmailService(IOptions<EmailServiceConfiguration> emailServiceConfiguration, IServiceProvider serviceProvider)
            : base(serviceProvider)
        {
            _config = emailServiceConfiguration.Value;
        }

        public EmailAttachment CreateAttachmentFromPath(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new ArgumentException("Le chemin du fichier est requis.", nameof(filePath));
            }

            var fullPath = Path.GetFullPath(filePath);

            if (!File.Exists(fullPath))
            {
                throw new FileNotFoundException("Le fichier joint est introuvable.", fullPath);
            }

            if (!ContentTypeProvider.TryGetContentType(fullPath, out var contentType) || string.IsNullOrWhiteSpace(contentType))
            {
                contentType = "application/octet-stream";
            }

            return new EmailAttachment
            {
                FileName = Path.GetFileName(fullPath),
                Content = File.ReadAllBytes(fullPath),
                ContentType = contentType
            };
        }

        public Task SendEmailPasswordReset(string userEmail, string link)
        {
            if (string.IsNullOrWhiteSpace(link))
            {
                throw new ArgumentException("Le lien est requis.", nameof(link));
            }

            var body = BuildPasswordResetHtml(link);

            return SendEmail(
                userEmail,
                "Réinitialisation de votre mot de passe - Wagram One BackOffice",
                body,
                isBodyHtml: true,
                attachments: null);
        }

        public async Task SendEmail(
            string userEmail,
            string subject,
            string body,
            bool isBodyHtml = true,
            List<EmailAttachment>? attachments = null)
        {
            ValidateInputs(userEmail, subject, body);

            var message = BuildMessage(userEmail, subject, body, isBodyHtml, attachments);

            await SendAsync(message);
        }

        private static void ValidateInputs(string userEmail, string subject, string body)
        {
            if (string.IsNullOrWhiteSpace(userEmail))
            {
                throw new ArgumentException("L'email destinataire est requis.", nameof(userEmail));
            }

            if (!MailboxAddress.TryParse(userEmail, out _))
            {
                throw new ArgumentException("L'email destinataire est invalide.", nameof(userEmail));
            }

            if (string.IsNullOrWhiteSpace(subject))
            {
                throw new ArgumentException("Le sujet est requis.", nameof(subject));
            }

            if (string.IsNullOrWhiteSpace(body))
            {
                throw new ArgumentException("Le corps du message est requis.", nameof(body));
            }
        }

        private MimeMessage BuildMessage(
            string userEmail,
            string subject,
            string body,
            bool isBodyHtml,
            List<EmailAttachment>? attachments)
        {
            var message = new MimeMessage();

            var fromName = string.IsNullOrWhiteSpace(_config.FromName) ? string.Empty : _config.FromName;
            message.From.Add(new MailboxAddress(fromName, _config.From));

            message.To.Add(MailboxAddress.Parse(userEmail));
            message.Subject = subject;

            var builder = new BodyBuilder();
            SetBody(builder, body, isBodyHtml);
            AddAttachments(builder, attachments);

            message.Body = builder.ToMessageBody();
            return message;
        }

        private static void SetBody(BodyBuilder builder, string body, bool isBodyHtml)
        {
            if (isBodyHtml)
            {
                builder.HtmlBody = body;
                return;
            }

            builder.TextBody = body;
        }

        private static void AddAttachments(BodyBuilder builder, List<EmailAttachment>? attachments)
        {
            if (attachments is null || attachments.Count == 0)
            {
                return;
            }

            foreach (var attachment in attachments)
            {
                if (!IsValidAttachment(attachment))
                {
                    continue;
                }

                AddAttachment(builder, attachment!);
            }
        }

        private static bool IsValidAttachment(EmailAttachment? attachment)
        {
            return attachment is not null
                   && attachment.Content is not null
                   && attachment.Content.Length > 0
                   && !string.IsNullOrWhiteSpace(attachment.FileName);
        }

        private static void AddAttachment(BodyBuilder builder, EmailAttachment attachment)
        {
            if (!string.IsNullOrWhiteSpace(attachment.ContentType))
            {
                var parsed = MimeKit.ContentType.Parse(attachment.ContentType);
                builder.Attachments.Add(attachment.FileName, attachment.Content, parsed);
                return;
            }

            builder.Attachments.Add(attachment.FileName, attachment.Content);
        }

        private async Task SendAsync(MimeMessage message)
        {
            using var client = new SmtpClient();

            try
            {
                var secure = GetSecureSocketOptions(_config.Port);

                await client.ConnectAsync(_config.SmtpServer, _config.Port, secure);

                if (!string.IsNullOrWhiteSpace(_config.UserName))
                {
                    await client.AuthenticateAsync(_config.UserName, _config.Password);
                }

                await client.SendAsync(message);
            }
            catch (Exception)
            {
                _logger.LogError("Problème pour envoyer un email à {To}.", string.Join(", ", message.To));
                throw new CustomException("Une erreur est survenue lors de l'envoi de l'email.");
            }
            finally
            {
                if (client.IsConnected)
                {
                    await client.DisconnectAsync(true);
                }
            }
        }

        private static SecureSocketOptions GetSecureSocketOptions(int port)
        {
            return port switch
            {
                465 => SecureSocketOptions.SslOnConnect,
                587 => SecureSocketOptions.StartTls,
                _ => SecureSocketOptions.StartTlsWhenAvailable
            };
        }

        private static string BuildPasswordResetHtml(string link)
        {
            return $@"
                <html>
                  <body style=""font-family: Arial, sans-serif; font-size: 14px; color: #333333; background-color: #f5f5f5; margin: 0; padding: 0;"">
                    <table width=""100%"" cellpadding=""0"" cellspacing=""0"">
                      <tr>
                        <td align=""center"" style=""padding: 30px 15px;"">
                          <table width=""600"" cellpadding=""0"" cellspacing=""0"" style=""background-color: #ffffff; border-radius: 8px; overflow: hidden;"">
                            <tr>
                              <td style=""background-color: #0d6efd; color: #ffffff; padding: 16px 24px; font-size: 18px; font-weight: bold;"">
                                Wagram One - BackOffice
                              </td>
                            </tr>
                            <tr>
                              <td style=""padding: 24px;"">
                                <p>Bonjour,</p>
                                <p>Vous avez demandé la réinitialisation de votre mot de passe pour l'accès au BackOffice Wagram One.</p>
                                <p>Cliquez sur le bouton ci-dessous pour définir un nouveau mot de passe&nbsp;:</p>
                                <p style=""text-align: center; margin: 30px 0;"">
                                  <a href=""{link}""
                                     style=""display: inline-block; padding: 12px 24px; background-color: #0d6efd; color: #ffffff;
                                            text-decoration: none; border-radius: 4px; font-weight: bold;"">
                                    Réinitialiser mon mot de passe
                                  </a>
                                </p>
                                <p>
                                  Si le bouton ne fonctionne pas, copiez-collez le lien suivant dans votre navigateur&nbsp;:<br/>
                                  <a href=""{link}"">{link}</a>
                                </p>
                                <p style=""font-size: 12px; color: #666666; margin-top: 24px;"">
                                  Ce lien est valable pour une durée limitée et ne peut être utilisé qu'une seule fois.
                                  Si vous n'êtes pas à l'origine de cette demande, vous pouvez ignorer cet e-mail.
                                </p>
                              </td>
                            </tr>
                            <tr>
                              <td style=""background-color: #f0f0f0; padding: 16px 24px; font-size: 12px; color: #777777; text-align: center;"">
                                Cet e-mail a été envoyé automatiquement, merci de ne pas y répondre.
                              </td>
                            </tr>
                          </table>
                        </td>
                      </tr>
                    </table>
                  </body>
                </html>";
        }
    }
}


