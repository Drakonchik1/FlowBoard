using System.Diagnostics;
using FlowBoard.Application.Common.Exceptions;
using FlowBoard.Domain.Exceptions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FlowBoard.Application.Common.Behaviors;

/// <summary>
/// MediatR pipeline behavior that records request duration with structured logging.
/// Expected failures (validation, auth, not-found) log at Warning; unexpected at Error.
/// </summary>
public sealed class LoggingBehavior<TRequest, TResponse>(
    ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private const long SlowRequestMs = 500;

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        var sw = Stopwatch.StartNew();

        try
        {
            var response = await next(cancellationToken);
            sw.Stop();

            var level = sw.ElapsedMilliseconds > SlowRequestMs ? LogLevel.Warning : LogLevel.Debug;
            logger.Log(level,
                "Handled {RequestName} in {ElapsedMs}ms",
                requestName, sw.ElapsedMilliseconds);

            return response;
        }
        catch (Exception ex)
        {
            sw.Stop();
            var isExpected = ex is ValidationException or UnauthorizedException or ForbiddenException
                or NotFoundException or DomainException or ConflictException;

            var level = isExpected ? LogLevel.Warning : LogLevel.Error;
            logger.Log(level, ex,
                "Request {RequestName} failed after {ElapsedMs}ms",
                requestName, sw.ElapsedMilliseconds);
            throw;
        }
    }
}
