using MediatR;
using OrderApi.Features.Orders.GetOrders;

namespace OrderApi.Features.Orders.SearchOrders;

/// <summary>
/// MediatR query with optional filter parameters for searching orders.
/// All filters are optional — no filters returns all orders (same as GET /orders).
/// </summary>
public sealed record SearchOrdersQuery(
    DateTime? From,
    DateTime? To,
    Guid? ResellerId,
    Guid? CustomerId,
    string? Status,
    decimal? MinTotal,
    decimal? MaxTotal,
    int Page,
    int PageSize) : IRequest<PagedResult<OrderSummaryResponse>>;
