namespace FlowBoard.API.Configuration;

internal static class RedisConnectionExtensions
{
    /// <summary>
    /// Resolves the Redis connection string from configuration.
    /// Supports <c>ConnectionStrings:Redis</c>, <c>Redis:ConnectionString</c>, and the <c>REDIS_CONNECTION</c> env var.
    /// </summary>
    public static string? GetRedisConnectionString(this IConfiguration configuration) =>
        configuration.GetConnectionString("Redis")
        ?? configuration["Redis:ConnectionString"]
        ?? configuration["REDIS_CONNECTION"];
}
