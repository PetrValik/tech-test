namespace OrderApi.Common.Events;

/// <summary>
/// Published when an order is soft-deleted.
/// Consumers: billing service (cancel pending invoices), notification service.
/// </summary>
/// <param name="OrderId">Unique identifier of the deleted order.</param>
/// <param name="DeletedAt">UTC timestamp when the deletion occurred.</param>
public sealed record OrderDeletedEvent(
    Guid OrderId,
    DateTime DeletedAt);
