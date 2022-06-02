using Microsoft.AspNetCore.Identity.UI.Services;

using SendGrid;
using SendGrid.Helpers.Mail;

using exdrive_web.Configuration;

namespace WebPWrecover.Services 
{
    public class EmailSender : IEmailSender
    {
        private readonly ILogger _logger;

        public EmailSender(ILogger<EmailSender> logger)
        {
            _logger = logger;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string message)
        {
            if (string.IsNullOrEmpty(ConnectionStrings.GetSendGridKey()))
            {
                throw new Exception("Null SendGridKey");
            }

            await Execute(ConnectionStrings.GetSendGridKey(), subject, message, toEmail);
        }

        public async Task Execute(string apiKey, string subject, string message, string toEmail)
        {
            var client = new SendGridClient(apiKey);

            var msg = new SendGridMessage()
            {
                From = new EmailAddress("mailexdrive@gmail.com"),
                Subject = subject,
                PlainTextContent = message,
                HtmlContent = message
            };

            msg.AddTo(new EmailAddress(toEmail));

            msg.SetClickTracking(false, false);

            var response = await client.SendEmailAsync(msg);

            _logger.LogInformation(response.IsSuccessStatusCode
                                   ? $"Email to {toEmail} queued successfully!"
                                   : $"Failure Email to {toEmail}");
        }
    }

}