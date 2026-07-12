using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace FlowBoard.API.Configuration;

internal static class RateLimitPartitionKeys
{
    internal const string WritesPolicy = "writes";

    internal static string ForAuthenticatedWrites(HttpContext context)
    {
        var userId = context.User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
            ?? context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        return userId
            ?? context.Connection.RemoteIpAddress?.ToString()
            ?? "unknown";
    }
}
