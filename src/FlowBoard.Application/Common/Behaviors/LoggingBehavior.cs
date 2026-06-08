using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FlowBoard.Application.Common.Behaviors;

/// <summary>
/// MediatR pipeline behavior that records request duration with structured logging.
/// Single log line per request: Debug for happy path, Warning for &gt; 500ms, Error for exceptions.
/// Cuts log volume in half vs paired start/end logging while keeping observability.
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
            logger.LogError(ex,
                "Request {RequestName} failed after {ElapsedMs}ms",
                requestName, sw.ElapsedMilliseconds);
            throw;
        }
    }
}
