namespace OrderApi.Features.Orders;

/// <summary>
/// Generic paginated result wrapper returned by list endpoints.
/// </summary>
/// <param name="Items">The page of items.</param>
/// <param name="TotalCount">Total number of items across all pages.</param>
/// <param name="Page">Current page number (1-based).</param>
/// <param name="PageSize">Maximum items per page.</param>
public record PagedResult<T>(IReadOnlyList<T> Items, int TotalCount, int Page, int PageSize)
{
    /// <summary>
    /// Total number of pages, computed from TotalCount and PageSize.
    /// </summary>
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 0;
}
