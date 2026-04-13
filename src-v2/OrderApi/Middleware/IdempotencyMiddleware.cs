using Microsoft.EntityFrameworkCore;
using OrderApi.Infrastructure;
using OrderApi.Infrastructure.Entities;

namespace OrderApi.Middleware;

/// <summary>
/// Middleware that ensures POST requests with an Idempotency-Key header
/// are processed at most once. If the key was already seen, the cached response
/// is replayed without invoking the handler again.
/// </summary>
/// <param name="next">The next middleware delegate in the pipeline.</param>
public sealed class IdempotencyMiddleware(RequestDelegate next)
{
    /// <summary>
    /// Header name for idempotency key.
    /// </summary>
    public const string HeaderName = "Idempotency-Key";

    /// <summary>
    /// Error message returned when a request with the same Idempotency-Key is already in progress.
    /// </summary>
    private const string InProgressErrorMessage =
        "A request with this Idempotency-Key is already in progress. Retry after the original request completes.";

    /// <summary>
    /// Represents the status code value for a pending state.
    /// </summary>
    private const int PendingStatusCode = 0;

    /// <summary>
    /// Specifies the maximum number of pending poll attempts allowed before the operation is considered unsuccessful.
    /// </summary>
    private const int MaxPendingPollAttempts = 100;

    /// <summary>
    /// Specifies the delay, in milliseconds, to wait between polling operations when a task is pending.
    /// </summary>
    private const int PendingPollDelayMilliseconds = 50;

    /// <summary>
    /// Maximum response body size cached for idempotency replay (1 MB).
    /// </summary>
    private const int MaxResponseBodyBytes = 1 * 1024 * 1024;

    /// <summary>
    /// Intercepts POST requests that carry an Idempotency-Key header.
    /// Stores a pending record before executing the handler, then persists the final response.
    /// On a duplicate key, replays the previously recorded response instead of executing the handler again.
    /// </summary>
    /// <param name="context">The current HTTP context for the request being processed.</param>
    /// <param name="orderContext">The database context used to read and write idempotency records.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous middleware execution.</returns>
    public async Task InvokeAsync(HttpContext context, OrderContext orderContext)
    {
        if (!HttpMethods.IsPost(context.Request.Method))
        {
            await next(context);
            return;
        }

        var idempotencyKey = context.Request.Headers[HeaderName].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            await next(context);
            return;
        }

        if (idempotencyKey.Length > 64)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsJsonAsync(new { error = "Idempotency-Key must not exceed 64 characters." });
            return;
        }

        var reservation = new IdempotencyRecord
        {
            Key = idempotencyKey,
            StatusCode = PendingStatusCode,
            ResponseBody = string.Empty,
            CreatedAt = DateTime.UtcNow
        };

        orderContext.IdempotencyRecords.Add(reservation);

        try
        {
            await orderContext.SaveChangesAsync(context.RequestAborted);
        }
        catch (DbUpdateException)
        {
            orderContext.Entry(reservation).State = EntityState.Detached;
            await ReplayOrConflictAsync(context, orderContext, idempotencyKey, context.RequestAborted);
            return;
        }

        await CaptureAndPersistAsync(context, orderContext, reservation, context.RequestAborted);
    }

    /// <summary>
    /// Waits for a competing in-flight request to complete, then either replays its
    /// response or returns 409 Conflict if it is still pending after the poll limit.
    /// </summary>
    /// <param name="context">The current HTTP context; used to write the replayed or conflict response.</param>
    /// <param name="orderContext">The database context used to query the existing idempotency record.</param>
    /// <param name="key">The idempotency key whose record is being waited on.</param>
    /// <param name="cancellationToken">Token used to cancel waiting for the competing request.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    private async Task ReplayOrConflictAsync(
        HttpContext context, OrderContext orderContext, string key, CancellationToken cancellationToken)
    {
        var existing = await WaitForCompletedRecordAsync(orderContext, key, cancellationToken);
        if (existing is null)
        {
            // Record vanished between the save attempt and the lookup — re-throw is not
            // available here, so surface as a conflict to the caller.
            context.Response.StatusCode = StatusCodes.Status409Conflict;
            await context.Response.WriteAsJsonAsync(new
            {
                error = InProgressErrorMessage
            }, cancellationToken);
            return;
        }

        if (existing.StatusCode == PendingStatusCode)
        {
            context.Response.StatusCode = StatusCodes.Status409Conflict;
            await context.Response.WriteAsJsonAsync(new
            {
                error = InProgressErrorMessage
            }, cancellationToken);
            return;
        }

        context.Response.StatusCode = existing.StatusCode;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsync(existing.ResponseBody, cancellationToken);
    }

    /// <summary>
    /// Buffers the downstream response body, persists it alongside the status code,
    /// then flushes the buffer to the original response stream.
    /// </summary>
    /// <param name="context">The current HTTP context; provides the response stream.</param>
    /// <param name="orderContext">The database context used to persist the captured response.</param>
    /// <param name="reservation">The pending idempotency record created before the handler ran.</param>
    /// <param name="cancellationToken">Token used to cancel the capture or persist operations.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    private async Task CaptureAndPersistAsync(
        HttpContext context, OrderContext orderContext, IdempotencyRecord reservation, CancellationToken cancellationToken)
    {
        var originalStream = context.Response.Body;
        await using var memoryStream = new MemoryStream();
        context.Response.Body = memoryStream;

        try
        {
            await next(context);

            if (memoryStream.Length > MaxResponseBodyBytes)
            {
                await RemoveOversizedReservationAsync(orderContext, reservation, memoryStream, originalStream, cancellationToken);
                return;
            }

            await PersistCapturedResponseAsync(context, orderContext, reservation, memoryStream, originalStream, cancellationToken);
        }
        finally
        {
            context.Response.Body = originalStream;
        }
    }

    /// <summary>
    /// Removes the pending reservation when the response body exceeds <see cref="MaxResponseBodyBytes"/>,
    /// then flushes the buffered body to the original stream.
    /// </summary>
    /// <param name="orderContext">The database context used to remove the oversized reservation.</param>
    /// <param name="reservation">The pending idempotency record to remove.</param>
    /// <param name="memoryStream">The in-memory buffer holding the response body.</param>
    /// <param name="originalStream">The original response stream to flush the body into.</param>
    /// <param name="cancellationToken">Token used to cancel the database or stream operations.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    private static async Task RemoveOversizedReservationAsync(
        OrderContext orderContext, IdempotencyRecord reservation,
        MemoryStream memoryStream, Stream originalStream, CancellationToken cancellationToken)
    {
        // Response too large to cache — remove the pending record so future requests
        // are not permanently blocked waiting for a completion that will never be saved.
        orderContext.IdempotencyRecords.Remove(reservation);
        await orderContext.SaveChangesAsync(cancellationToken);
        memoryStream.Seek(0, SeekOrigin.Begin);
        await memoryStream.CopyToAsync(originalStream, cancellationToken);
    }

    /// <summary>
    /// Reads the buffered response body, persists the status code and body on the reservation,
    /// then copies the buffer to the original response stream.
    /// </summary>
    /// <param name="context">The current HTTP context; provides the final response status code.</param>
    /// <param name="orderContext">The database context used to save the completed idempotency record.</param>
    /// <param name="reservation">The pending idempotency record to update with the captured response.</param>
    /// <param name="memoryStream">The in-memory buffer containing the response body bytes.</param>
    /// <param name="originalStream">The original response stream to copy the buffer into.</param>
    /// <param name="cancellationToken">Token used to cancel the database or stream operations.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    private static async Task PersistCapturedResponseAsync(
        HttpContext context, OrderContext orderContext, IdempotencyRecord reservation,
        MemoryStream memoryStream, Stream originalStream, CancellationToken cancellationToken)
    {
        memoryStream.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(memoryStream, leaveOpen: true);
        reservation.StatusCode = context.Response.StatusCode;
        reservation.ResponseBody = await reader.ReadToEndAsync(cancellationToken);
        await orderContext.SaveChangesAsync(cancellationToken);

        memoryStream.Seek(0, SeekOrigin.Begin);
        await memoryStream.CopyToAsync(originalStream, cancellationToken);
    }

    /// <summary>
    /// Polls the database until the idempotency record for <paramref name="key"/> reaches a
    /// non-pending state, or until <see cref="MaxPendingPollAttempts"/> is exhausted.
    /// </summary>
    /// <param name="orderContext">The database context used to query idempotency records.</param>
    /// <param name="key">The idempotency key to look up.</param>
    /// <param name="cancellationToken">Token used to cancel the polling loop.</param>
    /// <returns>
    /// The completed <see cref="IdempotencyRecord"/>, a still-pending record if the poll limit was reached,
    /// or <see langword="null"/> if the record no longer exists.
    /// </returns>
    private static async Task<IdempotencyRecord?> WaitForCompletedRecordAsync(
        OrderContext orderContext, string key, CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < MaxPendingPollAttempts; attempt++)
        {
            var existing = await orderContext.IdempotencyRecords
                .AsNoTracking()
                .FirstOrDefaultAsync(record => record.Key == key, cancellationToken);

            if (existing is null || existing.StatusCode != PendingStatusCode)
            {
                return existing;
            }

            await Task.Delay(PendingPollDelayMilliseconds, cancellationToken);
        }

        return await orderContext.IdempotencyRecords
            .AsNoTracking()
            .FirstOrDefaultAsync(record => record.Key == key, cancellationToken);
    }
}
