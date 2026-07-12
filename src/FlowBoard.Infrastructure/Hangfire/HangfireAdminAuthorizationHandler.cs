using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace FlowBoard.Infrastructure.Hangfire;

internal sealed class HangfireAdminAuthorizationHandler(
    IOptions<HangfireSettings> settings) : AuthorizationHandler<HangfireAdminRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        HangfireAdminRequirement requirement)
    {
        if (context.User.Identity?.IsAuthenticated != true)
            return Task.CompletedTask;

        var email = context.User.FindFirst(JwtRegisteredClaimNames.Email)?.Value
            ?? context.User.FindFirst(ClaimTypes.Email)?.Value;

        if (string.IsNullOrWhiteSpace(email))
            return Task.CompletedTask;

        var adminEmails = settings.Value.DashboardAdminEmails;
        if (adminEmails.Contains(email, StringComparer.OrdinalIgnoreCase))
            context.Succeed(requirement);

        return Task.CompletedTask;
    }
}
