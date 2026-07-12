using FlowBoard.Infrastructure.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace FlowBoard.UnitTests.Infrastructure;

public sealed class SmtpEmailServiceTests
{
    [Fact]
    public async Task SendEmailAsync_WhenHostNotConfigured_ThrowsSmtpNotConfiguredException()
    {
        var service = CreateService(new SmtpSettings { Host = "" });

        await Assert.ThrowsAsync<SmtpNotConfiguredException>(() =>
            service.SendEmailAsync("user@example.com", "Subject", "Body"));
    }

    [Fact]
    public async Task SendEmailAsync_WhenFromEmailNotConfigured_ThrowsSmtpNotConfiguredException()
    {
        var service = CreateService(new SmtpSettings
        {
            Host = "smtp.example.com",
            FromEmail = ""
        });

        await Assert.ThrowsAsync<SmtpNotConfiguredException>(() =>
            service.SendEmailAsync("user@example.com", "Subject", "Body"));
    }

    private static SmtpEmailService CreateService(SmtpSettings settings) =>
        new(Options.Create(settings), NullLogger<SmtpEmailService>.Instance);
}
