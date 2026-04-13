namespace OrderApi.Features.Orders.GetOrderById;

/// <summary>
/// Full detail view of a single order including all line items.
/// </summary>
public record OrderDetailResponse(
    Guid Id, Guid ResellerId, Guid CustomerId, Guid StatusId, string StatusName,
    DateTime CreatedDate, decimal TotalCost, decimal TotalPrice,
    IReadOnlyList<OrderItemResponse> Items, string ConcurrencyStamp);
