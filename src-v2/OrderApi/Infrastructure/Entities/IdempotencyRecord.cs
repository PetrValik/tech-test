namespace OrderApi.Infrastructure.Entities;

/// <summary>
/// Stores the result of a previously processed POST request, keyed by the
/// Idempotency-Key header. Allows replaying the same response
/// without executing the handler twice.
/// </summary>
public class IdempotencyRecord
{
    /// <summary>
    /// Value of the Idempotency-Key header (primary key).
    /// </summary>
    public required string Key { get; set; }

    /// <summary>
    /// HTTP status code that was returned to the client.
    /// </summary>
    public int StatusCode { get; set; }

    /// <summary>
    /// JSON response body that was returned to the client.
    /// </summary>
    public required string ResponseBody { get; set; }

    /// <summary>
    /// UTC timestamp when the record was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }
}
