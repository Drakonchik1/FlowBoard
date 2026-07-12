using FlowBoard.Infrastructure.Services;
using Hangfire;
using Microsoft.Extensions.Logging;

namespace FlowBoard.Infrastructure.Hangfire.Jobs;

/// <summary>
/// Hangfire job that sends a single email via SMTP. Failures are retried automatically.
/// </summary>
internal sealed class SendEmailJob(
    SmtpEmailService smtpEmailService,
    ILogger<SendEmailJob> logger)
{
    [AutomaticRetry(Attempts = 3)]
    public async Task ExecuteAsync(string to, string subject, string body, bool isHtml)
    {
        try
        {
            await smtpEmailService.SendEmailAsync(to, subject, body, isHtml, CancellationToken.None);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send email to {Recipient}", to);
            throw;
        }
    }
}
