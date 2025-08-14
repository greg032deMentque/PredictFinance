using BackPredictFinance.Common;
using BackPredictFinance.Common.Email;
using Microsoft.Extensions.Options;
using System.Net.Mail;


namespace BackPredictFinance.Services
{
    public class EmailService : BaseService
    {
        private readonly EmailServiceConfiguration _emailServiceConfiguration;

        public EmailService(IOptions<EmailServiceConfiguration> emailServiceConfiguration, IServiceProvider serviceProvider) : base(serviceProvider)
        {
            _emailServiceConfiguration = emailServiceConfiguration.Value;
        }

        public async Task SendEmailPasswordReset(string userEmail, string link)
        {
            MailMessage mailMessage = new MailMessage();
            mailMessage.From = new MailAddress(_emailServiceConfiguration.From);
            mailMessage.To.Add(new MailAddress(userEmail));

            mailMessage.Subject = "Wagram One - BackOffice - Password Reset";
            mailMessage.IsBodyHtml = true;
            mailMessage.Body = $"Hello, <br/> <br/> Click on this link to reset your password:</br> </br> <a href='{link}'>{link}</a>";

            using var client = new SmtpClient
            {
                Credentials = new System.Net.NetworkCredential(
                    _emailServiceConfiguration.UserName,
                    _emailServiceConfiguration.Password),
                Host = _emailServiceConfiguration.SmtpServer,
                Port = _emailServiceConfiguration.Port
            };

            try
            {
                await client.SendMailAsync(mailMessage);

            }
            catch (Exception ex)
            {
                var error = $"Problème pour envoyer un email à {userEmail}. ";

                _logger.LogError(error, ex);

                throw new CustomException(ex.Message);
            }
        }

        public async Task SendEmailWithAttachment(
            string userEmail,
            string subject,
            string htmlBody,
            Stream? fileStream,
            string attachmentFileName)
        {
            var mailMessage = new MailMessage
            {
                From = new MailAddress(_emailServiceConfiguration.From),
                Subject = subject,
                IsBodyHtml = true,
                Body = htmlBody
            };
            mailMessage.To.Add(new MailAddress(userEmail));

            // Attachement de fichier si fourni
            if (fileStream != null && !string.IsNullOrWhiteSpace(attachmentFileName))
            {
                fileStream.Position = 0;
                mailMessage.Attachments.Add(new Attachment(fileStream, attachmentFileName));
            }

            using var client = new SmtpClient
            {
                Credentials = new System.Net.NetworkCredential(
                    _emailServiceConfiguration.UserName,
                    _emailServiceConfiguration.Password),
                Host = _emailServiceConfiguration.SmtpServer,
                Port = _emailServiceConfiguration.Port
            };

            try
            {
                await client.SendMailAsync(mailMessage);
            }
            catch (Exception ex)
            {
                var error = $"Problème pour envoyer un email à {userEmail}.";
                _logger.LogError(error, ex);
                throw new CustomException(ex.Message);
            }
        }

    }
}
