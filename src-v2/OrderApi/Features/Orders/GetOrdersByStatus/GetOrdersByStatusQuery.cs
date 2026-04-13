using MediatR;
using OrderApi.Features.Orders.GetOrders;

namespace OrderApi.Features.Orders.GetOrdersByStatus;

/// <summary>
/// MediatR query to fetch a paginated list of orders filtered by status name.
/// </summary>
public record GetOrdersByStatusQuery(string StatusName, int Page, int PageSize)
    : IRequest<PagedResult<OrderSummaryResponse>>;
