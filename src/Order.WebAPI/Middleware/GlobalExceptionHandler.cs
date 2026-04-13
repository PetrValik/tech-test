using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Order.WebAPI.Middleware;

/// <summary>
/// Global exception handler that converts unhandled exceptions to a consistent JSON 500 response.
/// Registered via <c>services.AddExceptionHandler</c> and <c>app.UseExceptionHandler()</c>.
/// </summary>
public sealed class GlobalExceptionHandler : IExceptionHandler
{
    /// <summary>
    /// Logger for recording unhandled exception details, including request method, path, and trace ID.
    /// </summary>
    private readonly ILogger<GlobalExceptionHandler> _logger;

    /// <summary>
    /// Creates a new instance with the supplied logger.
    /// </summary>
    /// <param name="logger">Logger for recording unhandled exception details.</param>
    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Catches any unhandled exception, logs it, and writes a generic 500 JSON body.
    /// </summary>
    /// <param name="httpContext">The current HTTP context.</param>
    /// <param name="exception">The unhandled exception.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True – this handler always handles the exception.</returns>
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        _logger.LogError(exception, "Unhandled exception processing {Method} {Path} [TraceId={TraceId}]",
            httpContext.Request.Method, httpContext.Request.Path, httpContext.TraceIdentifier);

        if (httpContext.Response.HasStarted)
        {
            return true;
        }

        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
        await httpContext.Response.WriteAsJsonAsync(
            new { error = "An unexpected error occurred." },
            cancellationToken);

        return true;
    }
}
