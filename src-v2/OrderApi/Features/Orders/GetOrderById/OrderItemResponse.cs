namespace OrderApi.Features.Orders.GetOrderById;

/// <summary>
/// A single line item within an order detail response.
/// </summary>
public record OrderItemResponse(
    Guid Id, Guid OrderId, Guid ServiceId, string ServiceName,
    Guid ProductId, string ProductName, int Quantity,
    decimal UnitCost, decimal UnitPrice, decimal TotalCost, decimal TotalPrice);
