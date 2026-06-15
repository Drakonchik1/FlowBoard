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
/// Requires a running Docker daemon. CI runners provide one; locally, start Docker Desktop.
/// </summary>
public sealed class SqlServerFixture : IAsyncLifetime
{
    private readonly MsSqlContainer _container = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest")
        .Build();

    public IServiceProvider Services { get; private set; } = null!;

    public TestCurrentUserService CurrentUser { get; } = new();

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = _container.GetConnectionString(),
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

        Services = services.BuildServiceProvider();

        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FlowBoardDbContext>();
        await db.Database.MigrateAsync();
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

        await _container.DisposeAsync();
    }
}

[CollectionDefinition(nameof(SqlServerCollection))]
public sealed class SqlServerCollection : ICollectionFixture<SqlServerFixture>;
