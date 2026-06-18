namespace FlowBoard.API.Configuration;

internal static class SignalRRedisExtensions
{
    /// <summary>
    /// Adds the SignalR Redis backplane when a Redis connection string is configured.
    /// Supports <c>ConnectionStrings:Redis</c>, <c>Redis:ConnectionString</c>, and the <c>REDIS_CONNECTION</c> env var.
    /// </summary>
    public static IServiceCollection AddSignalRWithOptionalRedisBackplane(
        this IServiceCollection services,
        string? redisConnectionString)
    {
        var signalRBuilder = services.AddSignalR();

        if (!string.IsNullOrEmpty(redisConnectionString))
            signalRBuilder.AddStackExchangeRedis(redisConnectionString);

        return services;
    }
}
