using System.Diagnostics;
using MediatR;

namespace OrderApi.Common.Behaviors;

/// <summary>
/// MediatR pipeline behavior that logs the request name and execution duration.
/// Warnings are emitted for requests that exceed <see cref="SlowThresholdMs"/>.
/// </summary>
/// <param name="logger">The logger used to write request timing entries.</param>
public sealed class LoggingBehavior<TRequest, TResponse>(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    /// <summary>
    /// Threshold in milliseconds for logging a warning about slow requests.
    /// </summary>
    private const long SlowThresholdMs = 500;

    /// <summary>
    /// Logs the request name on entry, invokes the next handler, then logs the elapsed time.
    /// Emits a warning if execution exceeds <see cref="SlowThresholdMs"/> milliseconds.
    /// </summary>
    /// <param name="request">The incoming MediatR request being handled.</param>
    /// <param name="next">The delegate representing the next handler in the pipeline.</param>
    /// <param name="cancellationToken">Token used to cancel the downstream handler.</param>
    /// <returns>A <see cref="Task{TResult}"/> containing the response produced by the next handler.</returns>
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        logger.LogInformation("Handling {RequestName}", requestName);

        var stopwatch = Stopwatch.StartNew();
        var response = await next(cancellationToken);
        stopwatch.Stop();

        if (stopwatch.ElapsedMilliseconds > SlowThresholdMs)
        {
            logger.LogWarning("Slow request {RequestName} took {ElapsedMs}ms", requestName, stopwatch.ElapsedMilliseconds);
        }
        else
        {
            logger.LogInformation("Handled {RequestName} in {ElapsedMs}ms", requestName, stopwatch.ElapsedMilliseconds);
        }

        return response;
    }
}
