using FlowBoard.Infrastructure.Hangfire.Jobs;
using FlowBoard.Infrastructure.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace FlowBoard.UnitTests.Infrastructure;

public sealed class SendEmailJobTests
{
    [Fact]
    public async Task ExecuteAsync_WhenSmtpNotConfigured_ThrowsSoHangfireCanRetry()
    {
        var smtpEmailService = new SmtpEmailService(
            Options.Create(new SmtpSettings { Host = "" }),
            NullLogger<SmtpEmailService>.Instance);

        var job = new SendEmailJob(smtpEmailService, NullLogger<SendEmailJob>.Instance);

        await Assert.ThrowsAsync<SmtpNotConfiguredException>(() =>
            job.ExecuteAsync("user@example.com", "Subject", "Body", isHtml: true));
    }
}
