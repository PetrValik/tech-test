namespace OrderApi.Common.Events;

/// <summary>
/// Published when a new order is successfully created.
/// Consumers: billing service (calculate invoice), notification service (send confirmation email).
/// </summary>
/// <param name="OrderId">Unique identifier of the newly created order.</param>
/// <param name="ResellerId">Reseller that owns the order.</param>
/// <param name="CustomerId">End customer for the order.</param>
/// <param name="OccurredAt">UTC timestamp when the event occurred.</param>
public sealed record OrderCreatedEvent(
    Guid OrderId,
    Guid ResellerId,
    Guid CustomerId,
    DateTimeOffset OccurredAt);
