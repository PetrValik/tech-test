using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Order.WebAPI.Middleware;

/// <summary>
/// Ensures every request has a correlation ID for distributed tracing.
/// Reads <c>X-Correlation-ID</c> from the incoming request header, or generates
/// a new GUID if none is provided. The ID is added to the response headers
/// and injected into the logger scope so all log entries for the request share it.
/// </summary>
public sealed class CorrelationIdMiddleware
{
    /// <summary>
    /// Header name used for correlation ID propagation.
    /// </summary>
    public const string HeaderName = "X-Correlation-ID";

    /// <summary>
    /// The next middleware in the pipeline, invoked after processing the correlation ID.
    /// </summary>
    private readonly RequestDelegate _next;

    /// <summary>
    /// Provides logging capabilities for the CorrelationIdMiddleware component.
    /// </summary>
    private readonly ILogger<CorrelationIdMiddleware> _logger;

    /// <summary>
    /// Creates a new instance with the given pipeline delegate and logger.
    /// </summary>
    /// <param name="next">The next middleware in the pipeline.</param>
    /// <param name="logger">Logger for recording correlation ID details.</param>
    public CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
    {
        _next   = next;
        _logger = logger;
    }

    /// <summary>
    /// Reads or generates a correlation ID, enriches the log scope, and forwards the request.
    /// </summary>
    /// <param name="context">The current HTTP context.</param>
    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Request.Headers[HeaderName].ToString();
        if (string.IsNullOrEmpty(correlationId))
        {
            correlationId = Guid.NewGuid().ToString("D");
        }

        context.Response.OnStarting(() =>
        {
            context.Response.Headers[HeaderName] = correlationId;
            return Task.CompletedTask;
        });

        using (_logger.BeginScope(new Dictionary<string, object> { ["CorrelationId"] = correlationId }))
        {
            await _next(context);
        }
    }
}
