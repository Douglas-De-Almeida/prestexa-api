namespace PrestexaAPI.Services
{
    public interface IEmailSender
    {
        Task SendEmailAsync(
            string toEmail,
            string subject,
            string plainTextContent,
            string htmlContent);
    }
}