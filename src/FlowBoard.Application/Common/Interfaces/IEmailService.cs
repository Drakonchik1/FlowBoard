namespace FlowBoard.Application.Common.Interfaces;

/// <summary>
/// Sends transactional email via SMTP (MailKit implementation in Infrastructure).
/// </summary>
public interface IEmailService
{
    /// <summary>Sends an email to a single recipient.</summary>
    Task SendEmailAsync(
        string to,
        string subject,
        string body,
        bool isHtml = true,
        CancellationToken cancellationToken = default);
}
