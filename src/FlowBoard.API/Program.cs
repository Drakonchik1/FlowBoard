using System.Net;
using System.Threading.RateLimiting;
using FlowBoard.API.Configuration;
using FlowBoard.API.Hubs;
using FlowBoard.API.Middleware;
using FlowBoard.API.OpenApi;
using FlowBoard.API.Services;
using FlowBoard.Application;
using FlowBoard.Application.Common.Interfaces;
using FlowBoard.Infrastructure;
using FlowBoard.Infrastructure.Hangfire;
using FlowBoard.Infrastructure.Persistence;
using Hangfire;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddHangfireWithSqlServer(builder.Configuration);
builder.Services.AddHangfireAdminPolicy();
builder.Services.AddJwtBearerProblemDetails();
builder.Services.AddJwtBearerSignalR();

var redisConnection = builder.Configuration.GetRedisConnectionString();

builder.Services.AddControllers(options =>
    options.Conventions.Add(new WriteRateLimitingConvention()));
builder.Services.AddSignalRWithOptionalRedisBackplane(redisConnection);
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddSingleton<BoardGroupMembershipRegistry>();
builder.Services.AddSingleton<BoardHubJoinRateLimiter>();
builder.Services.AddSingleton<IBoardRealtimeNotifier, BoardRealtimeNotifier>();
builder.Services.AddSingleton<IBoardRealtimeGroupEvictor, BoardRealtimeGroupEvictor>();

var healthChecks = builder.Services.AddHealthChecks()
    .AddDbContextCheck<FlowBoardDbContext>(
        name: "database",
        tags: ["ready"]);

if (!string.IsNullOrWhiteSpace(redisConnection))
{
    healthChecks.AddRedis(redisConnection, name: "redis", tags: ["ready"]);
}

builder.Services.AddOpenApi(options => OpenApiConfiguration.Configure(options));

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = async (context, cancellationToken) =>
    {
        await ProblemDetailsWriter.WriteAsync(
            context.HttpContext,
            StatusCodes.Status429TooManyRequests,
            "Too many requests",
            "Rate limit exceeded. Try again later.");
    };

    options.AddPolicy("auth", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));

    options.AddPolicy(RateLimitPartitionKeys.WritesPolicy, context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: RateLimitPartitionKeys.ForAuthenticatedWrites(context),
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 60,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));
});

if (!builder.Environment.IsDevelopment())
{
    var allowedOrigins = builder.Configuration
        .GetSection("AllowedOrigins")
        .Get<string[]>() ?? [];

    if (allowedOrigins.Length == 0 || allowedOrigins.All(string.IsNullOrWhiteSpace))
    {
        throw new InvalidOperationException(
            "Production requires AllowedOrigins. Set AllowedOrigins__0 (and __1, ...) via environment or appsettings.Production.json.");
    }
}

builder.Services.AddCors(options =>
{
    if (builder.Environment.IsDevelopment())
    {
        options.AddDefaultPolicy(policy =>
            policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
    }
    else
    {
        var allowedOrigins = builder.Configuration
            .GetSection("AllowedOrigins")
            .Get<string[]>() ?? [];

        options.AddDefaultPolicy(policy =>
            policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod().AllowCredentials());
    }
});

var forwardedHeadersEnabled = builder.Configuration.GetValue("ForwardedHeaders:Enabled", false);
if (forwardedHeadersEnabled)
{
    builder.Services.Configure<ForwardedHeadersOptions>(options =>
    {
        options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;

        var knownProxies = builder.Configuration.GetSection("ForwardedHeaders:KnownProxies").Get<string[]>() ?? [];
        foreach (var proxy in knownProxies)
        {
            if (IPAddress.TryParse(proxy, out var address))
                options.KnownProxies.Add(address);
        }
    });
}

var app = builder.Build();

if (!string.IsNullOrWhiteSpace(redisConnection))
{
    var startupLogger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("FlowBoard.Startup");
    startupLogger.LogWarning(
        "Redis SignalR backplane is enabled. Board group eviction is process-local — deploy a single API replica until distributed eviction is implemented.");
}

if (forwardedHeadersEnabled)
    app.UseForwardedHeaders();

app.UseMiddleware<ExceptionHandlingMiddleware>();

var useTlsTermination = forwardedHeadersEnabled
    || builder.Configuration.GetValue("UseTls", false);

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();

    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<FlowBoardDbContext>();
    await db.Database.MigrateAsync();
}
else
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<FlowBoardDbContext>();
    var pending = await db.Database.GetPendingMigrationsAsync();
    if (pending.Any())
    {
        throw new InvalidOperationException(
            $"Database schema is out of date. Pending migrations: {string.Join(", ", pending)}. " +
            "Run 'dotnet ef database update' before starting Production traffic.");
    }

    if (useTlsTermination)
        app.UseHsts();
}

app.Use(async (context, next) =>
{
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["X-Frame-Options"] = "DENY";
    context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    context.Response.Headers["X-Permitted-Cross-Domain-Policies"] = "none";
    await next();
});

if (useTlsTermination)
    app.UseHttpsRedirection();
app.UseCors();
app.UseAuthentication();
app.UseRateLimiter();
app.UseAuthorization();
app.MapControllers();
app.MapHub<BoardHub>("/hubs/board");
app.MapHangfireDashboard("/jobs").RequireAuthorization("Admin");
if (app.Configuration.GetValue("Hangfire:RegisterRecurringJobs", true))
    app.RegisterHangfireRecurringJobs();

app.MapHealthChecks("/health/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = _ => false
});

app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});

app.Run();

public partial class Program;
