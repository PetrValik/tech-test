namespace OrderApi.Features.Orders;

/// <summary>
/// Shared query-string parameters for paginated list endpoints.
/// Used with [AsParameters] in Minimal API to bind page/pageSize
/// from the query string into a single strongly-typed object.
/// </summary>
/// <param name="Page">1-based page number (default 1, max 1,000,000).</param>
/// <param name="PageSize">Number of items per page (default 50, max 200).</param>
public record PaginationQuery(int Page = 1, int PageSize = 50)
{
    private const int MaxPageNumber = 1_000_000;
    private const int MaxPageSize = 200;

    /// <summary>
    /// Validates page and pageSize bounds.
    /// Returns a human-readable error message on failure, or <see langword="null"/> when parameters are valid.
    /// </summary>
    /// <returns>An error message string if validation fails; <see langword="null"/> if the parameters are valid.</returns>
    public string? Validate()
        => Page < 1 || Page > MaxPageNumber || PageSize < 1 || PageSize > MaxPageSize
            ? $"page must be between 1 and {MaxPageNumber:N0}; pageSize must be between 1 and {MaxPageSize}."
            : null;
}
