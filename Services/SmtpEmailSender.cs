using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Logging;
using MimeKit;
using MailKit.Net.Smtp;
using MailKit.Security;
using System;
using System.Threading.Tasks;

namespace PracticalAssignment.Services
{
    public class SmtpEmailSender : IEmailSender
    {
        private readonly string _host;
        private readonly int _port;
        private readonly string _username;
        private readonly string _password;
        private readonly ILogger<SmtpEmailSender> _logger;

        public SmtpEmailSender(string host, int port, string username, string password, ILogger<SmtpEmailSender> logger)
        {
            _host = host;
            _port = port;
            _username = username;
            _password = password;
            _logger = logger;
        }

        public async Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            try
            {
                _logger.LogInformation("Preparing to send email to {Email}", email);

                var mimeMessage = new MimeMessage();
                mimeMessage.From.Add(new MailboxAddress("Your Friendly App", _username));
                mimeMessage.To.Add(new MailboxAddress("", email));
                mimeMessage.Subject = subject;

                mimeMessage.Body = new TextPart("html")
                {
                    Text = htmlMessage
                };

                using var client = new SmtpClient();
                await client.ConnectAsync(_host, _port, SecureSocketOptions.SslOnConnect); // Use SecureSocketOptions.StartTls for port 587
                _logger.LogInformation("Connected to SMTP server");

                await client.AuthenticateAsync(_username, _password);
                _logger.LogInformation("Authenticated with SMTP server");

                await client.SendAsync(mimeMessage);
                _logger.LogInformation("Email sent successfully to {Email}", email);

                await client.DisconnectAsync(true);
                _logger.LogInformation("Disconnected from SMTP server");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending email to {Email}", email);
                throw;
            }
        }
    }
}
