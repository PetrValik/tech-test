namespace OrderApi.Features.Orders.CreateOrder;

/// <summary>
/// A single line item within a create-order command.
/// </summary>
/// <param name="ProductId">GUID of the product to order.</param>
/// <param name="Quantity">Number of units; must be greater than zero.</param>
public record CreateOrderItemRequest(Guid ProductId, int Quantity);
