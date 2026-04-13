using MediatR;

namespace OrderApi.Features.Orders.GetOrderHistory;

/// <summary>
/// MediatR query to retrieve the status change history for a specific order.
/// </summary>
public sealed record GetOrderHistoryQuery(Guid OrderId, int Page = 1, int PageSize = 50)
    : IRequest<PagedResult<OrderStatusHistoryResponse>>;
