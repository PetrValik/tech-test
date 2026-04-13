using MediatR;
using Microsoft.EntityFrameworkCore;
using OrderApi.Features.Orders.GetOrders;
using OrderApi.Infrastructure;

namespace OrderApi.Features.Orders.GetDeletedOrders;

/// <summary>
/// Returns soft-deleted orders by bypassing the global query filter.
/// </summary>
/// <param name="orderContext">The database context used to query soft-deleted orders.</param>
public sealed class GetDeletedOrdersHandler(OrderContext orderContext): IRequestHandler<GetDeletedOrdersQuery, PagedResult<OrderSummaryResponse>>
{
    /// <summary>
    /// Returns soft-deleted orders by bypassing the global IsDeleted query filter.
    /// </summary>
    /// <param name="request">The query containing pagination parameters.</param>
    /// <param name="cancellationToken">Token used to cancel the database query.</param>
    /// <returns>A <see cref="Task{TResult}"/> containing a <see cref="PagedResult{T}"/> of soft-deleted <see cref="OrderSummaryResponse"/> records.</returns>
    public async Task<PagedResult<OrderSummaryResponse>> Handle(GetDeletedOrdersQuery request, CancellationToken cancellationToken)
    {
        var query = orderContext.Orders
            .IgnoreQueryFilters()
            .Where(order => order.IsDeleted);

        return await OrderProjections.ToPagedSummaryAsync(query, request.Page, request.PageSize, cancellationToken);
    }
}
