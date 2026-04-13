using MediatR;

namespace OrderApi.Features.Orders.GetOrderById;

/// <summary>
/// MediatR query to fetch a single order by its GUID.
/// </summary>
public record GetOrderByIdQuery(Guid OrderId) : IRequest<OrderDetailResponse?>;
