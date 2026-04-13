using Group2.SWP391.SportsBicycles.Common.DTOs;
using Group2.SWP391.SportsBicycles.Services.Contract;
using MailKit.Net.Smtp;
using Microsoft.Extensions.Options;
using MimeKit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Group2.SWP391.SportsBicycles.Services.Implementation
{
    public class EmailService : IEmailService
    {
        private readonly EmailSettings _settings;

        public EmailService(IOptions<EmailSettings> options)
        {
            _settings = options.Value;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string body, bool isHtml = true)
        {
            var msg = new MimeMessage();
            msg.From.Add(new MailboxAddress(_settings.SenderName, _settings.SenderEmail));
            msg.To.Add(MailboxAddress.Parse(toEmail));
            msg.Subject = subject;

            msg.Body = isHtml
                ? new TextPart(MimeKit.Text.TextFormat.Html) { Text = body }
                : new TextPart("plain") { Text = body };

            using var smtp = new SmtpClient();

            await smtp.ConnectAsync(
                _settings.SmtpServer,
                _settings.Port,
                MailKit.Security.SecureSocketOptions.StartTlsWhenAvailable);

            await smtp.AuthenticateAsync(_settings.Username, _settings.Password);
            await smtp.SendAsync(msg);
            await smtp.DisconnectAsync(true);
        }
    }
}
