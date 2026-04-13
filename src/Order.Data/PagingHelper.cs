using Order.Model;
using System;
using System.Collections.Generic;

namespace Order.Data;

/// <summary>
/// Stateless helpers for page-number validation and empty-result construction.
/// </summary>
internal static class PagingHelper
{
    /// <summary>
    /// Maximum allowed page size to prevent excessively large queries and to keep skip offsets within
    /// safe integer arithmetic bounds.
    /// </summary>
    private const int MaxPageSize = 200;

    /// <summary>
    /// Clamps <paramref name="page"/> to a minimum of 1 and <paramref name="pageSize"/>
    /// to the range [1, <see cref="MaxPageSize"/>].
    /// </summary>
    /// <param name="page">The requested page number.</param>
    /// <param name="pageSize">The requested page size.</param>
    /// <returns>A tuple with the normalised <c>Page</c> and <c>PageSize</c> values.</returns>
    internal static (int Page, int PageSize) NormalizePage(int page, int pageSize)
        => (Math.Max(page, 1), Math.Clamp(pageSize, 1, MaxPageSize));

    /// <summary>
    /// Returns <see langword="true"/> when <paramref name="page"/> would cause an arithmetic
    /// overflow when computing the skip offset against the maximum pageSize.
    /// </summary>
    internal static bool IsPageOverflow(int page)
    {
        const int maxSafePage = int.MaxValue / MaxPageSize;
        return page > maxSafePage;
    }

    /// <summary>
    /// Constructs an empty paged result that preserves the requested page/pageSize for the caller.
    /// </summary>
    internal static PagedResult<OrderSummary> EmptyPagedResult(int page, int pageSize) =>
        new PagedResult<OrderSummary>
        {
            Items = [],
            TotalCount = 0,
            Page = page,
            PageSize = pageSize
        };
}
