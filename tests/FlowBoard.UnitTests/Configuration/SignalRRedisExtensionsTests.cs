using FlowBoard.API.Configuration;
using FlowBoard.API.Hubs;
using FlowBoard.Domain.Interfaces;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace FlowBoard.UnitTests.Configuration;

public sealed class SignalRRedisExtensionsTests
{
    [Fact]
    public void AddSignalRWithOptionalRedisBackplane_WithoutRedisConnectionString_RegistersBoardHub()
    {
        var configuration = new ConfigurationBuilder().Build();
        var services = CreateServices(configuration);
        var provider = services.BuildServiceProvider();

        var hubContext = provider.GetRequiredService<IHubContext<BoardHub, IBoardHubClient>>();
        var hub = ActivatorUtilities.CreateInstance<BoardHub>(provider);
        var lifetimeManager = provider.GetRequiredService<HubLifetimeManager<BoardHub>>();

        Assert.NotNull(hubContext);
        Assert.NotNull(hub);
        Assert.DoesNotContain("Redis", lifetimeManager.GetType().Name, StringComparison.Ordinal);
    }

    [Fact]
    public void AddSignalRWithOptionalRedisBackplane_WithRedisConnectionString_RegistersBoardHubWithBackplane()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Redis"] = "localhost:6379,abortConnect=false"
            })
            .Build();

        var services = CreateServices(configuration);
        var provider = services.BuildServiceProvider();

        var hubContext = provider.GetRequiredService<IHubContext<BoardHub, IBoardHubClient>>();
        var hub = ActivatorUtilities.CreateInstance<BoardHub>(provider);
        var lifetimeManager = provider.GetRequiredService<HubLifetimeManager<BoardHub>>();

        Assert.NotNull(hubContext);
        Assert.NotNull(hub);
        Assert.Contains("Redis", lifetimeManager.GetType().Name, StringComparison.Ordinal);
    }

    private static ServiceCollection CreateServices(IConfiguration configuration)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(Mock.Of<IBoardRepository>());
        services.AddSingleton(Mock.Of<IWorkspaceRepository>());
        services.AddSignalRWithOptionalRedisBackplane(configuration);
        return services;
    }
}
