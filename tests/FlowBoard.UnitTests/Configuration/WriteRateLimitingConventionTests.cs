using System.Reflection;
using FlowBoard.API.Configuration;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ActionConstraints;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.RateLimiting;

namespace FlowBoard.UnitTests.Configuration;

public sealed class WriteRateLimitingConventionTests
{
    private readonly WriteRateLimitingConvention _convention = new();

    [Fact]
    public void Apply_AddsWritePolicy_ToAuthorizedPostAction()
    {
        var action = CreateAction<AuthorizedController>(
            nameof(AuthorizedController.Action),
            httpMethods: [HttpMethods.Post]);

        _convention.Apply(action);

        Assert.Contains(
            action.Selectors.SelectMany(selector => selector.EndpointMetadata),
            metadata => metadata is EnableRateLimitingAttribute rateLimit
                && rateLimit.PolicyName == RateLimitPartitionKeys.WritesPolicy);
    }

    [Fact]
    public void Apply_DoesNotAddWritePolicy_ToAuthorizedGetAction()
    {
        var action = CreateAction<AuthorizedController>(
            nameof(AuthorizedController.Action),
            httpMethods: [HttpMethods.Get]);

        _convention.Apply(action);

        Assert.DoesNotContain(
            action.Selectors.SelectMany(selector => selector.EndpointMetadata),
            metadata => metadata is EnableRateLimitingAttribute);
    }

    [Fact]
    public void Apply_DoesNotAddWritePolicy_ToAnonymousMutationAction()
    {
        var action = CreateAction<AuthorizedController>(
            nameof(AuthorizedController.AnonymousAction),
            httpMethods: [HttpMethods.Post]);

        _convention.Apply(action);

        Assert.DoesNotContain(
            action.Selectors.SelectMany(selector => selector.EndpointMetadata),
            metadata => metadata is EnableRateLimitingAttribute);
    }

    [Fact]
    public void Apply_DoesNotAddWritePolicy_ToUnauthenticatedController()
    {
        var action = CreateAction<PublicController>(
            nameof(PublicController.Action),
            httpMethods: [HttpMethods.Post]);

        _convention.Apply(action);

        Assert.DoesNotContain(
            action.Selectors.SelectMany(selector => selector.EndpointMetadata),
            metadata => metadata is EnableRateLimitingAttribute);
    }

    private static ActionModel CreateAction<TController>(
        string actionName,
        IReadOnlyList<string> httpMethods)
    {
        var controllerAttributes = typeof(TController).GetCustomAttributes(inherit: true).Cast<object>().ToList();
        var controllerModel = new ControllerModel(typeof(TController).GetTypeInfo(), controllerAttributes)
        {
            ControllerName = typeof(TController).Name
        };

        var method = typeof(TController).GetMethod(
            actionName,
            BindingFlags.Public | BindingFlags.Instance)!;

        var actionAttributes = method.GetCustomAttributes(inherit: true).Cast<object>().ToList();
        var actionModel = new ActionModel(method, actionAttributes)
        {
            Controller = controllerModel,
            ActionName = actionName
        };

        actionModel.Selectors.Add(new SelectorModel
        {
            ActionConstraints = { new HttpMethodActionConstraint(httpMethods) }
        });

        return actionModel;
    }

    [Authorize]
    private sealed class AuthorizedController : ControllerBase
    {
        public IActionResult Action() => Ok();

        [AllowAnonymous]
        public IActionResult AnonymousAction() => Ok();
    }

    private sealed class PublicController : ControllerBase
    {
        public IActionResult Action() => Ok();
    }
}
