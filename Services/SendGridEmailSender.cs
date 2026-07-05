using SendGrid;
using SendGrid.Helpers.Mail;

namespace PrestexaAPI.Services
{
    public class SendGridEmailSender : IEmailSender
    {
        private readonly IConfiguration _config;
        private readonly ILogger<SendGridEmailSender> _logger;

        public SendGridEmailSender(
            IConfiguration config,
            ILogger<SendGridEmailSender> logger)
        {
            _config = config;
            _logger = logger;
        }

        public async Task SendEmailAsync(
            string toEmail,
            string subject,
            string plainTextContent,
            string htmlContent)
        {
            var apiKey =
                _config["SENDGRID_API_KEY"] ??
                _config["SendGrid:ApiKey"];

            var fromEmail =
                _config["SENDGRID_FROM_EMAIL"] ??
                _config["SendGrid:FromEmail"] ??
                "no-reply@prestexa.com";

            var fromName =
                _config["SENDGRID_FROM_NAME"] ??
                _config["SendGrid:FromName"] ??
                "Prestexa";

            if (string.IsNullOrWhiteSpace(apiKey))
            {
                throw new InvalidOperationException("SendGrid API key is missing.");
            }

            var client = new SendGridClient(apiKey);

            var from = new EmailAddress(fromEmail, fromName);
            var to = new EmailAddress(toEmail);

            var message = MailHelper.CreateSingleEmail(
                from,
                to,
                subject,
                plainTextContent,
                htmlContent
            );

            var response = await client.SendEmailAsync(message);

            if (!response.IsSuccessStatusCode)
            {
                var responseBody = await response.Body.ReadAsStringAsync();

                _logger.LogError(
                    "SendGrid failed. StatusCode: {StatusCode}. Body: {Body}",
                    response.StatusCode,
                    responseBody
                );

                throw new InvalidOperationException("Unable to send email.");
            }
        }
    }
}