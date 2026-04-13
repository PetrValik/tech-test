using MediatR;
using OrderApi.Infrastructure;

namespace OrderApi.Features.Orders.GetOrders;

/// <summary>
/// Returns a paginated list of all orders, sorted newest-first.
/// </summary>
/// <param name="orderContext">The database context used to query orders.</param>
public sealed class GetOrdersHandler(OrderContext orderContext)
    : IRequestHandler<GetOrdersQuery, PagedResult<OrderSummaryResponse>>
{
    /// <summary>
    /// Returns a paginated list of all active orders, sorted newest-first.
    /// </summary>
    /// <param name="query">The pagination query containing page number and page size.</param>
    /// <param name="cancellationToken">Token used to cancel the database query.</param>
    /// <returns>A <see cref="Task{TResult}"/> containing a <see cref="PagedResult{T}"/> of <see cref="OrderSummaryResponse"/> records.</returns>
    public Task<PagedResult<OrderSummaryResponse>> Handle(GetOrdersQuery query, CancellationToken cancellationToken)
        => OrderProjections.ToPagedSummaryAsync(orderContext.Orders, query.Page, query.PageSize, cancellationToken);
}
