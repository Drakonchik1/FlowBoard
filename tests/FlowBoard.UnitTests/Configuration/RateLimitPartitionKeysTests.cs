using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using FlowBoard.API.Configuration;
using Microsoft.AspNetCore.Http;

namespace FlowBoard.UnitTests.Configuration;

public sealed class RateLimitPartitionKeysTests
{
    [Fact]
    public void ForAuthenticatedWrites_UsesSubClaim_WhenPresent()
    {
        var userId = Guid.NewGuid();
        var context = CreateContext(
            user: CreateUser(JwtRegisteredClaimNames.Sub, userId.ToString()),
            remoteIp: IPAddress.Parse("203.0.113.10"));

        Assert.Equal(userId.ToString(), RateLimitPartitionKeys.ForAuthenticatedWrites(context));
    }

    [Fact]
    public void ForAuthenticatedWrites_FallsBackToIp_WhenUserIsAnonymous()
    {
        var context = CreateContext(
            user: new ClaimsPrincipal(new ClaimsIdentity()),
            remoteIp: IPAddress.Parse("203.0.113.10"));

        Assert.Equal("203.0.113.10", RateLimitPartitionKeys.ForAuthenticatedWrites(context));
    }

    [Fact]
    public void ForAuthenticatedWrites_FallsBackToUnknown_WhenUserAndIpMissing()
    {
        var context = CreateContext(user: new ClaimsPrincipal(new ClaimsIdentity()));

        Assert.Equal("unknown", RateLimitPartitionKeys.ForAuthenticatedWrites(context));
    }

    private static HttpContext CreateContext(ClaimsPrincipal user, IPAddress? remoteIp = null)
    {
        var context = new DefaultHttpContext
        {
            User = user
        };

        if (remoteIp is not null)
            context.Connection.RemoteIpAddress = remoteIp;

        return context;
    }

    private static ClaimsPrincipal CreateUser(string claimType, string claimValue) =>
        new(new ClaimsIdentity([new Claim(claimType, claimValue)], authenticationType: "Bearer"));
}
