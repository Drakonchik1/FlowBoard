namespace FlowBoard.Infrastructure.Hangfire;

public sealed class HangfireSettings
{
    public const string SectionName = "Hangfire";

    /// <summary>JWT email claims allowed to access the Hangfire dashboard at /jobs.</summary>
    public string[] DashboardAdminEmails { get; init; } = [];
}
