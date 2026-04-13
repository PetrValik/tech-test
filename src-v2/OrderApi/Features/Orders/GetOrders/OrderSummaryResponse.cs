namespace OrderApi.Features.Orders.GetOrders;

/// <summary>
/// Summary view of a single order, returned by list endpoints.
/// </summary>
public record OrderSummaryResponse(
    Guid Id,
    Guid ResellerId,
    Guid CustomerId,
    Guid StatusId,
    string StatusName,
    DateTime CreatedDate,
    int ItemCount,
    decimal TotalCost,
    decimal TotalPrice);
