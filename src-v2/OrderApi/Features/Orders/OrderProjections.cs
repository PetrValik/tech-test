using Microsoft.EntityFrameworkCore;
using OrderApi.Features.Orders.GetOrders;
using OrderApi.Infrastructure.Entities;

namespace OrderApi.Features.Orders;

/// <summary>
/// Shared query helpers reused by list-style order endpoints.
/// Aggregates (TotalCost, TotalPrice) are computed server-side to avoid N+1 loads.
/// </summary>
public static class OrderProjections
{
    /// <summary>
    /// Applies pagination to an order query and returns a typed paged result.
    /// Orders are sorted newest-first; totals are calculated via SQL-side projections.
    /// </summary>
    /// <param name="query">The base IQueryable to page over.</param>
    /// <param name="page">1-based page number.</param>
    /// <param name="pageSize">Maximum number of items to return.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A PagedResult containing the requested slice of OrderSummaryResponse records.</returns>
    public static async Task<PagedResult<OrderSummaryResponse>> ToPagedSummaryAsync(
        IQueryable<Order> query, int page, int pageSize, CancellationToken cancellationToken)
    {
        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .AsNoTracking()
            .OrderByDescending(order => order.CreatedDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(order => new OrderSummaryResponse(
                new Guid(order.Id),
                new Guid(order.ResellerId),
                new Guid(order.CustomerId),
                new Guid(order.StatusId),
                order.Status.Name,
                order.CreatedDate,
                order.Items.Count(),
                order.Items.Sum(item => (decimal)(item.Quantity ?? 0) * item.Product.UnitCost),
                order.Items.Sum(item => (decimal)(item.Quantity ?? 0) * item.Product.UnitPrice)
            ))
            .ToListAsync(cancellationToken);

        return new PagedResult<OrderSummaryResponse>(items, totalCount, page, pageSize);
    }
}
