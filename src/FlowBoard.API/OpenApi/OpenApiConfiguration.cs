using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace FlowBoard.API.OpenApi;

internal static class OpenApiConfiguration
{
    public static OpenApiOptions Configure(OpenApiOptions options)
    {
        options.AddDocumentTransformer((document, _, _) =>
        {
            document.Info = new OpenApiInfo
            {
                Title = "FlowBoard API",
                Version = "v1",
                Description = "Project management SaaS API — Auth and Workspaces (Sprints 1–2)."
            };

            document.Components ??= new OpenApiComponents();
            document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
            document.Components.SecuritySchemes["Bearer"] = new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                Description = "JWT access token from /api/auth/login or /api/auth/register."
            };

            document.Security ??= [];
            document.Security.Add(new OpenApiSecurityRequirement
            {
                [new OpenApiSecuritySchemeReference("Bearer")] = []
            });

            return Task.CompletedTask;
        });

        return options;
    }
}
