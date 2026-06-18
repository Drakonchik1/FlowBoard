using DotNet.Testcontainers.Builders;
using FlowBoard.Application;
using FlowBoard.Application.Common.Interfaces;
using FlowBoard.Infrastructure;
using FlowBoard.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.MsSql;

namespace FlowBoard.IntegrationTests;

/// <summary>
/// Spins up a real SQL Server in Docker via Testcontainers, wires the full Application + Infrastructure
/// service graph against it, and applies EF Core migrations. Tests then drive the system through MediatR
/// exactly as the API does — no mocks, real SQL, real Dapper reads.
///
/// Requires a running Docker daemon (TestContainers) unless <c>FLOWBOARD_INTEGRATION_CONNECTION</c> is set
/// (see <c>scripts/run-integration-tests.ps1 -UseCompose</c>). When Docker is unavailable,
/// <see cref="IsDockerAvailable"/> is false and integration tests skip instead of failing the suite.
/// </summary>
public sealed class SqlServerFixture : IAsyncLifetime
{
    private MsSqlContainer? _container;

    public bool IsDockerAvailable { get; private set; }

    public IServiceProvider Services { get; private set; } = null!;

    public TestCurrentUserService CurrentUser { get; } = new();

    public CapturingBoardRealtimeNotifier RealtimeNotifier { get; } = new();

    public async Task InitializeAsync()
    {
        try
        {
            var connectionString = Environment.GetEnvironmentVariable("FLOWBOARD_INTEGRATION_CONNECTION");
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                _container = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest")
                    .Build();

                await _container.StartAsync();
                connectionString = _container.GetConnectionString();
            }

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:DefaultConnection"] = connectionString,
                    ["Jwt:SecretKey"] = "integration-tests-secret-key-0123456789",
                    ["Jwt:Issuer"] = "FlowBoard.API",
                    ["Jwt:Audience"] = "FlowBoard.Client",
                })
                .Build();

            var services = new ServiceCollection();
            services.AddLogging();
            services.AddSingleton<IConfiguration>(configuration);
            services.AddApplication();
            services.AddInfrastructure(configuration);
            services.AddSingleton<ICurrentUserService>(CurrentUser);
            services.AddSingleton<IBoardRealtimeNotifier>(RealtimeNotifier);
            services.AddSingleton<IBoardRealtimeGroupEvictor, NoOpBoardRealtimeGroupEvictor>();

            Services = services.BuildServiceProvider();

            using var scope = Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<FlowBoardDbContext>();
            await db.Database.MigrateAsync();

            IsDockerAvailable = true;
        }
        catch (Exception ex) when (IsDockerUnavailable(ex))
        {
            IsDockerAvailable = false;
        }
    }

    /// <summary>Sends a request through MediatR on a fresh DI scope (fresh DbContext), as the API would per request.</summary>
    public async Task<TResponse> SendAsync<TResponse>(IRequest<TResponse> request)
    {
        using var scope = Services.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();
        return await sender.Send(request);
    }

    public async Task SendAsync(IRequest request)
    {
        using var scope = Services.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();
        await sender.Send(request);
    }

    public async Task ExecuteDbAsync(Func<FlowBoardDbContext, Task> action)
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FlowBoardDbContext>();
        await action(db);
    }

    public async Task DisposeAsync()
    {
        if (Services is IAsyncDisposable disposable)
            await disposable.DisposeAsync();

        if (_container is not null)
            await _container.DisposeAsync();
    }

    private static bool IsDockerUnavailable(Exception ex) =>
        ex is DockerUnavailableException ||
        (ex is AggregateException aggregate && aggregate.InnerExceptions.Any(IsDockerUnavailable)) ||
        (ex.InnerException is not null && IsDockerUnavailable(ex.InnerException));
}

[CollectionDefinition(nameof(SqlServerCollection))]
public sealed class SqlServerCollection : ICollectionFixture<SqlServerFixture>;
