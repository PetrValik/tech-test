namespace OrderApi.Common.Events;

/// <summary>
/// Published when an order's status transitions to a new value.
/// Consumers: notification service (status update email), analytics service.
/// </summary>
/// <param name="OrderId">Order whose status changed.</param>
/// <param name="NewStatus">The status name the order transitioned to.</param>
/// <param name="OccurredAt">UTC timestamp when the event occurred.</param>
public sealed record OrderStatusChangedEvent(
    Guid OrderId,
    string NewStatus,
    DateTimeOffset OccurredAt);
