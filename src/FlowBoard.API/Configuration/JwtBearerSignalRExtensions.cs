using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace FlowBoard.API.Configuration;

internal static class JwtBearerSignalRExtensions
{
    /// <summary>
    /// Reads the JWT from the <c>access_token</c> query string for SignalR negotiate/WebSocket requests.
    /// </summary>
    public static IServiceCollection AddJwtBearerSignalR(this IServiceCollection services)
    {
        services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
        {
            var existingOnMessageReceived = options.Events.OnMessageReceived;

            options.Events.OnMessageReceived = async context =>
            {
                if (existingOnMessageReceived is not null)
                    await existingOnMessageReceived(context);

                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;

                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
                    context.Token = accessToken;
            };
        });

        return services;
    }
}
