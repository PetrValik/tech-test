using MediatR;
using OrderApi.Features.Orders.GetOrders;
using OrderApi.Infrastructure;

namespace OrderApi.Features.Orders.GetOrdersByStatus;

/// <summary>
/// Returns a paginated list of orders filtered by status name.
/// </summary>
/// <param name="orderContext">The database context used to query orders filtered by status.</param>
public sealed class GetOrdersByStatusHandler(OrderContext orderContext)
    : IRequestHandler<GetOrdersByStatusQuery, PagedResult<OrderSummaryResponse>>
{
    /// <summary>
    /// Returns a paginated list of orders whose status name matches the query.
    /// Returns an empty result if the status name is not a recognised lifecycle value.
    /// </summary>
    /// <param name="query">The query containing the status name filter and pagination parameters.</param>
    /// <param name="cancellationToken">Token used to cancel the database query.</param>
    /// <returns>A <see cref="Task{TResult}"/> containing a <see cref="PagedResult{T}"/> of matching <see cref="OrderSummaryResponse"/> records.</returns>
    public Task<PagedResult<OrderSummaryResponse>> Handle(GetOrdersByStatusQuery query, CancellationToken cancellationToken)
    {
        var normalized = OrderStatusNames.All.FirstOrDefault(
            statusName => string.Equals(statusName, query.StatusName, StringComparison.OrdinalIgnoreCase));

        if (normalized is null)
        {
            return Task.FromResult(new PagedResult<OrderSummaryResponse>([], 0, query.Page, query.PageSize));
        }

        return OrderProjections.ToPagedSummaryAsync(
            orderContext.Orders.Where(order => order.Status.Name == normalized), query.Page, query.PageSize, cancellationToken);
    }
}
