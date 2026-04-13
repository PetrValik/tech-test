using MediatR;

namespace OrderApi.Features.Orders.CreateOrder;

/// <summary>
/// MediatR command for creating a new order. Also serves as the JSON request body.
/// </summary>
/// <param name="ResellerId">GUID of the reseller placing the order.</param>
/// <param name="CustomerId">GUID of the end customer.</param>
/// <param name="Items">One or more line items to include in the order.</param>
public record CreateOrderCommand(Guid ResellerId, Guid CustomerId, IReadOnlyList<CreateOrderItemRequest> Items)
    : IRequest<CreateOrderResult>;
