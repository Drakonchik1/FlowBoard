using FlowBoard.Application.Common.Interfaces;
using FlowBoard.Infrastructure.Hangfire.Jobs;
using Hangfire;

namespace FlowBoard.Infrastructure.Services;

/// <summary>
/// Non-blocking <see cref="IEmailService"/> that enqueues Hangfire jobs for SMTP delivery with retry.
/// </summary>
internal sealed class QueuedEmailService(IBackgroundJobClient backgroundJobClient) : IEmailService
{
    public Task SendEmailAsync(
        string to,
        string subject,
        string body,
        bool isHtml = true,
        CancellationToken cancellationToken = default)
    {
        backgroundJobClient.Enqueue<SendEmailJob>(job =>
            job.ExecuteAsync(to, subject, body, isHtml));

        return Task.CompletedTask;
    }
}
