using FlowBoard.API.Middleware;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace FlowBoard.API.Configuration;

internal static class JwtBearerProblemDetailsExtensions
{
    public static IServiceCollection AddJwtBearerProblemDetails(this IServiceCollection services)
    {
        services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
        {
            options.Events = new JwtBearerEvents
            {
                OnChallenge = async context =>
                {
                    context.HandleResponse();
                    await ProblemDetailsWriter.WriteAsync(
                        context.HttpContext,
                        StatusCodes.Status401Unauthorized,
                        "Unauthorized",
                        "Authentication is required.");
                },
                OnForbidden = async context =>
                {
                    await ProblemDetailsWriter.WriteAsync(
                        context.HttpContext,
                        StatusCodes.Status403Forbidden,
                        "Forbidden",
                        "You do not have permission to access this resource.");
                }
            };
        });

        return services;
    }
}
