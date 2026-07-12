using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using FlowBoard.Infrastructure.Hangfire;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace FlowBoard.UnitTests.Infrastructure;

public sealed class HangfireAdminAuthorizationHandlerTests
{
    [Fact]
    public async Task HandleRequirementAsync_WhenEmailIsConfiguredAdmin_Succeeds()
    {
        var handler = CreateHandler(["admin@flowboard.test"]);
        var context = CreateContext("admin@flowboard.test");

        await handler.HandleAsync(context);

        Assert.True(context.HasSucceeded);
    }

    [Fact]
    public async Task HandleRequirementAsync_WhenEmailIsNotAdmin_DoesNotSucceed()
    {
        var handler = CreateHandler(["admin@flowboard.test"]);
        var context = CreateContext("member@flowboard.test");

        await handler.HandleAsync(context);

        Assert.False(context.HasSucceeded);
    }

    [Fact]
    public async Task HandleRequirementAsync_WhenUnauthenticated_DoesNotSucceed()
    {
        var handler = CreateHandler(["admin@flowboard.test"]);
        var context = new AuthorizationHandlerContext(
            [new HangfireAdminRequirement()],
            new ClaimsPrincipal(new ClaimsIdentity()),
            resource: null);

        await handler.HandleAsync(context);

        Assert.False(context.HasSucceeded);
    }

    [Fact]
    public async Task HandleRequirementAsync_EmailMatchIsCaseInsensitive()
    {
        var handler = CreateHandler(["Admin@FlowBoard.Test"]);
        var context = CreateContext("admin@flowboard.test");

        await handler.HandleAsync(context);

        Assert.True(context.HasSucceeded);
    }

    private static HangfireAdminAuthorizationHandler CreateHandler(string[] adminEmails) =>
        new(Options.Create(new HangfireSettings { DashboardAdminEmails = adminEmails }));

    private static AuthorizationHandlerContext CreateContext(string email)
    {
        var identity = new ClaimsIdentity(
            [new Claim(JwtRegisteredClaimNames.Email, email)],
            authenticationType: "Bearer");

        return new AuthorizationHandlerContext(
            [new HangfireAdminRequirement()],
            new ClaimsPrincipal(identity),
            resource: null);
    }
}
