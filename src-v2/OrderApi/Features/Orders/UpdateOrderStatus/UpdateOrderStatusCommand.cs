using MediatR;

namespace OrderApi.Features.Orders.UpdateOrderStatus;

/// <summary>
/// MediatR command that combines the route parameter, request body, and optional optimistic-concurrency token.
/// </summary>
/// <param name="OrderId">The GUID of the order to update.</param>
/// <param name="StatusName">Target status name; must be one of the valid lifecycle values.</param>
/// <param name="IfMatch">Optional ETag from the If-Match header for optimistic concurrency.</param>
public record UpdateOrderStatusCommand(Guid OrderId, string StatusName, string? IfMatch = null) : IRequest<UpdateResult>;
