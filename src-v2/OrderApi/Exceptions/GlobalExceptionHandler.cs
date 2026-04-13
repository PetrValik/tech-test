using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;

namespace OrderApi.Exceptions;

/// <summary>
/// Global exception handler that converts unhandled exceptions to consistent JSON responses.
/// <see cref="ValidationException"/> → 400; everything else → 500.
/// Registered via services.AddExceptionHandler and app.UseExceptionHandler().
/// </summary>
/// <param name="logger">The logger used to record validation warnings and unhandled errors.</param>
public sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    : IExceptionHandler
{
    /// <summary>
    /// Catches any unhandled exception, logs it, and writes an appropriate JSON response.
    /// </summary>
    /// <param name="httpContext">The current HTTP context; used to write the error response.</param>
    /// <param name="exception">The unhandled exception that was thrown.</param>
    /// <param name="cancellationToken">Token used to cancel the response write operation.</param>
    /// <returns>
    /// A <see cref="ValueTask{TResult}"/> that resolves to true once the exception has been handled,
    /// signalling to the framework that no further exception handling is needed.
    /// </returns>
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (httpContext.Response.HasStarted)
        {
            return true;
        }

        if (exception is ValidationException validationException)
        {
            logger.LogWarning("Validation failed for {Method} {Path}: {Errors}",
                httpContext.Request.Method, httpContext.Request.Path,
                string.Join("; ", validationException.Errors.Select(validationError => validationError.ErrorMessage)));

            httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
            var errors = validationException.Errors
                .GroupBy(validationError => validationError.PropertyName)
                .ToDictionary(propertyGroup => propertyGroup.Key, propertyGroup => propertyGroup.Select(validationError => validationError.ErrorMessage).ToArray());

            await httpContext.Response.WriteAsJsonAsync(errors, cancellationToken);
            return true;
        }

        logger.LogError(exception, "Unhandled exception processing {Method} {Path} [TraceId={TraceId}]",
            httpContext.Request.Method, httpContext.Request.Path, httpContext.TraceIdentifier);

        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
        await httpContext.Response.WriteAsJsonAsync(
            new { error = "An unexpected error occurred." },
            cancellationToken);

        return true;
    }
}
