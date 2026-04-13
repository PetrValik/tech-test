namespace OrderApi.Features.Orders.UpdateOrderStatus;

/// <summary>
/// Request payload for updating an order's status (JSON body).
/// </summary>
/// <param name="StatusName">Target status name; must be one of the valid lifecycle values.</param>
public record UpdateOrderStatusRequest(string StatusName);
