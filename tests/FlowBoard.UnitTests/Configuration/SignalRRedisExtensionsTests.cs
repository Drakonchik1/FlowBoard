using FlowBoard.API.Configuration;
using FlowBoard.API.Hubs;
using FlowBoard.API.Services;
using MediatR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.StackExchangeRedis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;

namespace FlowBoard.UnitTests.Configuration;

public sealed class SignalRRedisExtensionsTests
{
    [Fact]
    public void AddSignalRWithOptionalRedisBackplane_WithoutRedisConnectionString_RegistersBoardHubWithoutRedisOptions()
    {
        var services = CreateServices(redisConnectionString: null);
        var provider = services.BuildServiceProvider();

        var hubContext = provider.GetRequiredService<IHubContext<BoardHub, IBoardHubClient>>();
        var hub = ActivatorUtilities.CreateInstance<BoardHub>(provider);

        Assert.NotNull(hubContext);
        Assert.NotNull(hub);
        var redisOptions = provider.GetService<IOptions<RedisOptions>>();
        if (redisOptions?.Value.Configuration is { } redisConfiguration)
            Assert.Empty(redisConfiguration.EndPoints);
    }

    [Fact]
    public void AddSignalRWithOptionalRedisBackplane_WithRedisConnectionString_RegistersBoardHubWithBackplaneOptions()
    {
        const string connectionString = "localhost:6379,abortConnect=false";

        var services = CreateServices(connectionString);
        var provider = services.BuildServiceProvider();

        var hubContext = provider.GetRequiredService<IHubContext<BoardHub, IBoardHubClient>>();
        var hub = ActivatorUtilities.CreateInstance<BoardHub>(provider);
        var redisOptions = provider.GetRequiredService<IOptions<RedisOptions>>().Value;

        Assert.NotNull(hubContext);
        Assert.NotNull(hub);
        Assert.NotNull(redisOptions.Configuration);
        var endpoint = Assert.IsType<System.Net.DnsEndPoint>(Assert.Single(redisOptions.Configuration!.EndPoints));
        Assert.Equal("localhost", endpoint.Host);
        Assert.Equal(6379, endpoint.Port);
        Assert.False(redisOptions.Configuration.AbortOnConnectFail);
    }

    private static ServiceCollection CreateServices(string? redisConnectionString)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(Mock.Of<ISender>());
        services.AddSingleton<BoardGroupMembershipRegistry>();
        services.AddSignalRWithOptionalRedisBackplane(redisConnectionString);
        return services;
    }
}
