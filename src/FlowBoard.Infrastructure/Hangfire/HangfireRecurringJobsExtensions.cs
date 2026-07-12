using FlowBoard.Infrastructure.Hangfire.Jobs;
using Hangfire;
using Microsoft.AspNetCore.Builder;

namespace FlowBoard.Infrastructure.Hangfire;

public static class HangfireRecurringJobsExtensions
{
    public static WebApplication RegisterHangfireRecurringJobs(this WebApplication app)
    {
        RecurringJob.AddOrUpdate<CleanupExpiredRefreshTokensJob>(
            "cleanup-expired-refresh-tokens",
            job => job.ExecuteAsync(),
            Cron.Daily);

        return app;
    }
}
