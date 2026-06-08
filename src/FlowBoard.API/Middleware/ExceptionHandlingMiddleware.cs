using System.Diagnostics;
using System.Text.Json;
using FlowBoard.Application.Common.Exceptions;
using FlowBoard.Domain.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace FlowBoard.API.Middleware;

/// <summary>
/// Global exception handler that converts exceptions to RFC 7807 Problem Details responses.
/// Every response carries a traceId so a developer can correlate it with the log line
/// that captured the same exception.
/// </summary>
public sealed class ExceptionHandlingMiddleware(
    RequestDelegate next,
    ILogger<ExceptionHandlingMiddleware> logger)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            var traceId = Activity.Current?.Id ?? context.TraceIdentifier;
            var isExpected = ex is ValidationException or UnauthorizedException or ForbiddenException
                or NotFoundException or DomainException or ConflictException;

            if (isExpected)
                logger.LogWarning(ex, "Request failed with expected error. TraceId={TraceId}", traceId);
            else
                logger.LogError(ex, "Unhandled exception. TraceId={TraceId}", traceId);

            await HandleExceptionAsync(context, ex, traceId);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception, string traceId)
    {
        var (statusCode, problem) = exception switch
        {
            ValidationException validationEx => (
                StatusCodes.Status422UnprocessableEntity,
                new ProblemDetails
                {
                    Title = "Validation failed",
                    Status = StatusCodes.Status422UnprocessableEntity,
                    Extensions =
                    {
                        ["errors"] = validationEx.Errors
                            .GroupBy(e => e.PropertyName)
                            .ToDictionary(
                                g => g.Key,
                                g => g.Select(e => e.ErrorMessage).ToArray())
                    }
                }),

            ConflictException conflictEx => (
                StatusCodes.Status409Conflict,
                new ProblemDetails
                {
                    Title = "Conflict",
                    Detail = conflictEx.Message,
                    Status = StatusCodes.Status409Conflict
                }),

            DomainException domainEx => (
                StatusCodes.Status400BadRequest,
                new ProblemDetails
                {
                    Title = "Business rule violation",
                    Detail = domainEx.Message,
                    Status = StatusCodes.Status400BadRequest
                }),

            UnauthorizedException unauthorizedEx => (
                StatusCodes.Status401Unauthorized,
                new ProblemDetails
                {
                    Title = "Unauthorized",
                    Detail = unauthorizedEx.Message,
                    Status = StatusCodes.Status401Unauthorized
                }),

            ForbiddenException forbiddenEx => (
                StatusCodes.Status403Forbidden,
                new ProblemDetails
                {
                    Title = "Forbidden",
                    Detail = forbiddenEx.Message,
                    Status = StatusCodes.Status403Forbidden
                }),

            NotFoundException notFoundEx => (
                StatusCodes.Status404NotFound,
                new ProblemDetails
                {
                    Title = "Not found",
                    Detail = notFoundEx.Message,
                    Status = StatusCodes.Status404NotFound
                }),

            _ => (
                StatusCodes.Status500InternalServerError,
                new ProblemDetails
                {
                    Title = "Internal server error",
                    Detail = "An unexpected error occurred.",
                    Status = StatusCodes.Status500InternalServerError
                })
        };

        problem.Instance = context.Request.Path;
        problem.Extensions["traceId"] = traceId;
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/problem+json";

        await context.Response.WriteAsync(JsonSerializer.Serialize(problem, JsonOptions));
    }
}
