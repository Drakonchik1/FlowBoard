using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.ActionConstraints;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.RateLimiting;

namespace FlowBoard.API.Configuration;

/// <summary>
/// Applies the authenticated write rate limit policy to mutation actions on [Authorize] controllers.
/// </summary>
internal sealed class WriteRateLimitingConvention : IActionModelConvention
{
    private static readonly HashSet<string> MutationMethods = new(StringComparer.OrdinalIgnoreCase)
    {
        HttpMethods.Post,
        HttpMethods.Put,
        HttpMethods.Patch,
        HttpMethods.Delete
    };

    public void Apply(ActionModel action)
    {
        if (!action.Controller.Attributes.OfType<AuthorizeAttribute>().Any())
            return;

        if (action.Attributes.OfType<AllowAnonymousAttribute>().Any())
            return;

        if (action.Attributes.OfType<EnableRateLimitingAttribute>().Any()
            || action.Controller.Attributes.OfType<EnableRateLimitingAttribute>().Any())
            return;

        var httpMethods = action.Selectors
            .SelectMany(selector => selector.ActionConstraints?.OfType<HttpMethodActionConstraint>() ?? [])
            .SelectMany(constraint => constraint.HttpMethods)
            .ToList();

        if (httpMethods.Count == 0 || !httpMethods.Any(MutationMethods.Contains))
            return;

        foreach (var selector in action.Selectors)
            selector.EndpointMetadata.Add(new EnableRateLimitingAttribute(RateLimitPartitionKeys.WritesPolicy));
    }
}
