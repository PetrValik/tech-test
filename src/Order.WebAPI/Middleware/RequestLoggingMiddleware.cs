using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Order.WebAPI.Middleware;

/// <summary>
/// Logs every HTTP request with method, path, status code, and elapsed time.
/// Place after <c>UseExceptionHandler</c> so that error responses are also captured.
/// </summary>
public sealed class RequestLoggingMiddleware
{
    /// <summary>
    /// The next middleware in the pipeline to invoke after logging the request.
    /// </summary>
    private readonly RequestDelegate _next;

    /// <summary>
    /// Provides logging capabilities for the request logging middleware.
    /// </summary>
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    /// <summary>
    /// Creates a new instance with the given pipeline delegate and logger.
    /// </summary>
    /// <param name="next">The next middleware in the pipeline.</param>
    /// <param name="logger">Logger for recording request details.</param>
    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        _next   = next;
        _logger = logger;
    }

    /// <summary>
    /// Invokes the next middleware and logs the request outcome.
    /// </summary>
    /// <param name="context">The current HTTP context.</param>
    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            await _next(context);
        }
        finally
        {
            stopwatch.Stop();

            _logger.LogInformation(
                "HTTP {Method} {Path} responded {StatusCode} in {ElapsedMs}ms",
                context.Request.Method,
                context.Request.Path,
                context.Response.StatusCode,
                stopwatch.ElapsedMilliseconds);
        }
    }
}
