namespace FlowBoard.Infrastructure.Services;

/// <summary>Thrown when SMTP is not configured so Hangfire can retry or surface the failure.</summary>
public sealed class SmtpNotConfiguredException(string message) : InvalidOperationException(message);
