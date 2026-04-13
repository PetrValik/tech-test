using MediatR;
using OrderApi.Features.Orders.GetOrders;

namespace OrderApi.Features.Orders.GetDeletedOrders;

/// <summary>
/// MediatR query to retrieve paginated soft-deleted orders.
/// </summary>
public sealed record GetDeletedOrdersQuery(int Page, int PageSize) : IRequest<PagedResult<OrderSummaryResponse>>;
