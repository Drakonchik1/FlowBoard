using FlowBoard.Infrastructure.Hangfire.Jobs;
using Hangfire;
using Hangfire.SqlServer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FlowBoard.Infrastructure.Hangfire;

public static class HangfireServiceExtensions
{
    public static IServiceCollection AddHangfireWithSqlServer(
        this IServiceCollection services,
        IConfiguration configuration,
        bool addServer = true)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "ConnectionStrings:DefaultConnection is required for Hangfire SQL Server storage.");

        services.Configure<HangfireSettings>(configuration.GetSection(HangfireSettings.SectionName));

        services.AddHangfire(config => config
            .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UseSqlServerStorage(connectionString, new SqlServerStorageOptions
            {
                CommandBatchMaxTimeout = TimeSpan.FromMinutes(5),
                SlidingInvisibilityTimeout = TimeSpan.FromMinutes(5),
                QueuePollInterval = TimeSpan.Zero,
                UseRecommendedIsolationLevel = true,
                DisableGlobalLocks = false
            }));

        services.AddScoped<SendEmailJob>();
        services.AddScoped<CleanupExpiredRefreshTokensJob>();

        if (addServer)
            services.AddHangfireServer();

        return services;
    }

    public static IServiceCollection AddHangfireAdminPolicy(this IServiceCollection services)
    {
        services.AddSingleton<IAuthorizationHandler, HangfireAdminAuthorizationHandler>();
        services.Configure<AuthorizationOptions>(options =>
        {
            options.AddPolicy("Admin", policy =>
                policy.RequireAuthenticatedUser()
                    .AddRequirements(new HangfireAdminRequirement()));
        });

        return services;
    }
}
