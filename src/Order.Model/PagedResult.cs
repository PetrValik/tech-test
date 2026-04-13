using System;
using System.Collections.Generic;

namespace Order.Model;

/// <summary>
/// Generic paginated result wrapper returned by list endpoints.
/// </summary>
/// <typeparam name="T">The item type.</typeparam>
public class PagedResult<T>
{
    /// <summary>
    /// The page of items returned for the current page number.
    /// </summary>
    public IReadOnlyList<T> Items { get; set; } = [];

    /// <summary>
    /// Total number of items across all pages.
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// Current page number (1-based).
    /// </summary>
    public int Page { get; set; }

    /// <summary>
    /// Maximum number of items per page.
    /// </summary>
    public int PageSize { get; set; }
}
