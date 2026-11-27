using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using MimeKit;
using MimeKit.Text;
using MailKit.Net.Smtp;
using ECommerce.Module.Core.Services;

namespace ECommerce.Module.EmailSenderSmtp
{
    public class EmailSender : IEmailSender
    {
        private readonly EmailConfig _emailConfig = new EmailConfig();

        public EmailSender(IConfiguration config)
        {
            _emailConfig.SmtpServer = config.GetValue<string>("SmtpServer");
            _emailConfig.SmtpUsername = config.GetValue<string>("SmtpUsername");
            _emailConfig.SmtpPassword = config.GetValue<string>("SmtpPassword");
            _emailConfig.SmtpPort = config.GetValue<int>("SmtpPort");
            //ValidateConfiguration();
        }

        private void ValidateConfiguration()
        {
            if (string.IsNullOrWhiteSpace(_emailConfig.SmtpServer))
            {
                throw new InvalidOperationException("SMTP server is not configured. Set SmtpServer in appsettings.json.");
            }

            if (_emailConfig.SmtpPort <= 0)
            {
                throw new InvalidOperationException("SMTP port is not configured. Set SmtpPort in appsettings.json.");
            }

            if (string.IsNullOrWhiteSpace(_emailConfig.SmtpUsername))
            {
                throw new InvalidOperationException("SMTP username is not configured. Set SmtpUsername in appsettings.json.");
            }

            if (string.IsNullOrWhiteSpace(_emailConfig.SmtpPassword))
            {
                throw new InvalidOperationException("SMTP password is not configured. Set SmtpPassword in appsettings.json.");
            }
        }
        public async Task SendEmailAsync(string email, string subject, string body, bool isHtml = false)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                throw new InvalidOperationException("Cannot send order email because the recipient email address is missing.");
            }

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_emailConfig.SmtpUsername, _emailConfig.SmtpUsername));
            message.To.Add(new MailboxAddress(email, email));
            message.Subject = subject;

            var textFormat = isHtml ? TextFormat.Html : TextFormat.Plain;
            message.Body = new TextPart(textFormat)
            {
                Text = body
            };

            using (var client = new SmtpClient())
            {
                // Accept all SSL certificates
                client.ServerCertificateValidationCallback = (s, c, h, e) => true;

                // Use STARTTLS for Gmail (port 587)
                await client.ConnectAsync(_emailConfig.SmtpServer, _emailConfig.SmtpPort, MailKit.Security.SecureSocketOptions.StartTls);

                // Remove XOAUTH2 if present
                client.AuthenticationMechanisms.Remove("XOAUTH2");

                // Authenticate with credentials
                await client.AuthenticateAsync(_emailConfig.SmtpUsername, _emailConfig.SmtpPassword);

                await client.SendAsync(message);
                await client.DisconnectAsync(true);
            }
        }
    }
}

