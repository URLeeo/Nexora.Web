using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;
using Nexora.Web.Models.EmailModels;

namespace Nexora.Web.Services.Email;

public class SmtpEmailSender : IEmailSender
{
    private readonly SmtpSettings _settings;

    public SmtpEmailSender(IOptions<SmtpSettings> options)
    {
        _settings = options.Value;
    }

    public async Task SendAsync(string toEmail, string subject, string htmlBody, CancellationToken ct = default)
    {
        using var message = new MailMessage();
        message.From = new MailAddress(_settings.FromEmail, _settings.FromName);
        message.To.Add(new MailAddress(toEmail));
        message.Subject = subject;
        message.Body = htmlBody;
        message.IsBodyHtml = true;

        using var client = new SmtpClient(_settings.Host, _settings.Port)
        {
            EnableSsl = _settings.EnableSsl,
            Credentials = new NetworkCredential(_settings.User, _settings.Password)
        };

        await client.SendMailAsync(message);
    }
}
