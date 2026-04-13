using MediatR;

namespace OrderApi.Features.Orders.GetOrders;

/// <summary>
/// MediatR query to fetch a paginated list of all orders.
/// </summary>
public record GetOrdersQuery(int Page, int PageSize) : IRequest<PagedResult<OrderSummaryResponse>>;
