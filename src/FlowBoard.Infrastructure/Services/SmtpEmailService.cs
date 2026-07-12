using FlowBoard.Application.Common.Interfaces;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;

namespace FlowBoard.Infrastructure.Services;

internal sealed class SmtpEmailService(
    IOptions<SmtpSettings> options,
    ILogger<SmtpEmailService> logger) : IEmailService
{
    private readonly SmtpSettings _settings = options.Value;

    public async Task SendEmailAsync(
        string to,
        string subject,
        string body,
        bool isHtml = true,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_settings.Host))
        {
            throw new SmtpNotConfiguredException(
                "SMTP is not configured (Smtp:Host is empty). Set Smtp__Host and related env vars.");
        }

        if (string.IsNullOrWhiteSpace(_settings.FromEmail))
        {
            throw new SmtpNotConfiguredException(
                "SMTP FromEmail is not configured. Set Smtp__FromEmail.");
        }

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_settings.FromName, _settings.FromEmail));
        message.To.Add(MailboxAddress.Parse(to));
        message.Subject = subject;
        message.Body = new TextPart(isHtml ? "html" : "plain")
        {
            Text = body
        };

        using var client = new SmtpClient();
        await client.ConnectAsync(
            _settings.Host,
            _settings.Port,
            GetSecureSocketOptions(_settings),
            cancellationToken);

        if (!string.IsNullOrWhiteSpace(_settings.Username))
        {
            await client.AuthenticateAsync(_settings.Username, _settings.Password, cancellationToken);
        }

        await client.SendAsync(message, cancellationToken);
        await client.DisconnectAsync(true, cancellationToken);

        logger.LogInformation("Email sent to {Recipient} with subject '{Subject}'.", to, subject);
    }

    private static SecureSocketOptions GetSecureSocketOptions(SmtpSettings settings)
    {
        if (!settings.UseSsl)
            return SecureSocketOptions.None;

        return settings.Port == 465
            ? SecureSocketOptions.SslOnConnect
            : SecureSocketOptions.StartTls;
    }
}
