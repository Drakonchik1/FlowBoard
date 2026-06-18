using FlowBoard.API.Configuration;
using Microsoft.Extensions.Configuration;

namespace FlowBoard.UnitTests.Configuration;

public sealed class RedisConnectionExtensionsTests
{
    [Fact]
    public void GetRedisConnectionString_FromConnectionStringsRedis_ReturnsValue()
    {
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["ConnectionStrings:Redis"] = "localhost:6379"
        });

        Assert.Equal("localhost:6379", configuration.GetRedisConnectionString());
    }

    [Fact]
    public void GetRedisConnectionString_FromRedisConnectionString_ReturnsValue()
    {
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["Redis:ConnectionString"] = "redis-host:6379"
        });

        Assert.Equal("redis-host:6379", configuration.GetRedisConnectionString());
    }

    [Fact]
    public void GetRedisConnectionString_FromRedisConnectionEnvVar_ReturnsValue()
    {
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["REDIS_CONNECTION"] = "env-redis:6379"
        });

        Assert.Equal("env-redis:6379", configuration.GetRedisConnectionString());
    }

    [Fact]
    public void GetRedisConnectionString_Precedence_ConnectionStringsRedisWins()
    {
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["ConnectionStrings:Redis"] = "primary:6379",
            ["Redis:ConnectionString"] = "secondary:6379",
            ["REDIS_CONNECTION"] = "tertiary:6379"
        });

        Assert.Equal("primary:6379", configuration.GetRedisConnectionString());
    }

    [Fact]
    public void GetRedisConnectionString_Precedence_RedisConnectionStringOverEnvVar()
    {
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["Redis:ConnectionString"] = "secondary:6379",
            ["REDIS_CONNECTION"] = "tertiary:6379"
        });

        Assert.Equal("secondary:6379", configuration.GetRedisConnectionString());
    }

    [Fact]
    public void GetRedisConnectionString_WhenUnset_ReturnsNull()
    {
        var configuration = BuildConfiguration(new Dictionary<string, string?>());

        Assert.Null(configuration.GetRedisConnectionString());
    }

    [Fact]
    public void GetRedisConnectionString_WhenWhitespaceOnly_ReturnsNull()
    {
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["ConnectionStrings:Redis"] = "   "
        });

        Assert.Null(configuration.GetRedisConnectionString());
    }

    [Fact]
    public void GetRedisConnectionString_TrimsLeadingAndTrailingWhitespace()
    {
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["ConnectionStrings:Redis"] = "  localhost:6379  "
        });

        Assert.Equal("localhost:6379", configuration.GetRedisConnectionString());
    }

    private static IConfiguration BuildConfiguration(Dictionary<string, string?> values) =>
        new ConfigurationBuilder().AddInMemoryCollection(values).Build();
}
