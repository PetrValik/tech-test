namespace OrderApi.Features.Orders.GetOrderHistory;

/// <summary>
/// Response record for a single status transition in the audit trail.
/// </summary>
public sealed record OrderStatusHistoryResponse(
    string FromStatus,
    string ToStatus,
    DateTime ChangedAt);
