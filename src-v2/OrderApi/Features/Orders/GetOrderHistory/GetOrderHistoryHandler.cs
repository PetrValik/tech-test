using MediatR;
using Microsoft.EntityFrameworkCore;
using OrderApi.Infrastructure;

namespace OrderApi.Features.Orders.GetOrderHistory;

/// <summary>
/// Returns the chronological list of status transitions for an order, with total count.
/// </summary>
/// <param name="orderContext">The database context used to query the status-history audit trail.</param>
public sealed class GetOrderHistoryHandler(OrderContext orderContext)
    : IRequestHandler<GetOrderHistoryQuery, PagedResult<OrderStatusHistoryResponse>>
{
    /// <summary>
    /// Returns chronological status-transition audit entries for the specified order.
    /// </summary>
    /// <param name="request">The query containing the order ID and pagination parameters.</param>
    /// <param name="cancellationToken">Token used to cancel the database query.</param>
    /// <returns>A <see cref="Task{TResult}"/> containing a <see cref="PagedResult{T}"/> of <see cref="OrderStatusHistoryResponse"/> entries sorted by change date.</returns>
    public async Task<PagedResult<OrderStatusHistoryResponse>> Handle(
        GetOrderHistoryQuery request, CancellationToken cancellationToken)
    {
        var orderIdBytes = request.OrderId.ToByteArray();
        var skip = (request.Page - 1) * request.PageSize;

        var historyQuery = orderContext.StatusHistory
            .AsNoTracking()
            .Where(historyEntry => historyEntry.OrderId == orderIdBytes);

        var totalCount = await historyQuery.CountAsync(cancellationToken);

        var items = await historyQuery
            .OrderBy(historyEntry => historyEntry.ChangedAt)
            .Skip(skip)
            .Take(request.PageSize)
            .Select(historyEntry => new OrderStatusHistoryResponse(
                historyEntry.FromStatus.Name,
                historyEntry.ToStatus.Name,
                historyEntry.ChangedAt))
            .ToListAsync(cancellationToken);

        return new PagedResult<OrderStatusHistoryResponse>(items, totalCount, request.Page, request.PageSize);
    }
}
