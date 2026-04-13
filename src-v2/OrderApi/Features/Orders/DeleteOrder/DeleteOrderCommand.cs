using MediatR;

namespace OrderApi.Features.Orders.DeleteOrder;

/// <summary>
/// MediatR command to soft-delete an order by ID.
/// </summary>
public sealed record DeleteOrderCommand(Guid OrderId) : IRequest<DeleteOrderResult>;
