using Serilog.Context;

namespace OrderApi.Middleware;

/// <summary>
/// Ensures every request has a correlation ID for distributed tracing.
/// Reads X-Correlation-ID from the incoming request header, or generates
/// a new GUID if none is provided. The ID is added to the response headers
/// and pushed into Serilog's <see cref="LogContext"/> so every log entry
/// emitted during the request automatically includes it.
/// </summary>
/// <param name="next">The next middleware delegate in the pipeline.</param>
public sealed class CorrelationIdMiddleware(RequestDelegate next)
{
    /// <summary>
    /// Header name used for correlation ID propagation.
    /// </summary>
    public const string HeaderName = "X-Correlation-ID";

    /// <summary>
    /// Reads or generates a correlation ID, enriches the Serilog LogContext, and forwards the request.
    /// </summary>
    /// <param name="context">The current HTTP context for the request being processed.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous middleware execution.</returns>
    public async Task InvokeAsync(HttpContext context)
    {
        var rawCorrelationId = context.Request.Headers[HeaderName].FirstOrDefault();
        var correlationId = IsValidCorrelationId(rawCorrelationId)
            ? rawCorrelationId!
            : Guid.NewGuid().ToString("D");

        context.Response.OnStarting(() =>
        {
            context.Response.Headers[HeaderName] = correlationId;
            return Task.CompletedTask;
        });

        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            await next(context);
        }
    }

    /// <summary>
    /// Returns true if the value is a non-empty string no longer than 128 characters
    /// that contains no ASCII control characters (prevents HTTP response header injection).
    /// </summary>
    /// <param name="value">The candidate correlation ID string to validate.</param>
    /// <returns>true if the value is safe to use as a correlation ID; otherwise false.</returns>
    private static bool IsValidCorrelationId(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length > 128)
        {
            return false;
        }

        foreach (var character in value)
        {
            if (char.IsControl(character))
            {
                return false;
            }
        }

        return true;
    }
}
