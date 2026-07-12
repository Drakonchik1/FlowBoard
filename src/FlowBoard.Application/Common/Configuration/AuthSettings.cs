namespace FlowBoard.Application.Common.Configuration;

public sealed class AuthSettings
{
    public const string SectionName = "Auth";

    /// <summary>When false, POST /api/auth/register returns 403 (recommended in Production).</summary>
    public bool AllowRegistration { get; set; } = true;
}
