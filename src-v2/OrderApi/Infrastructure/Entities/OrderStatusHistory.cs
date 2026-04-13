namespace OrderApi.Infrastructure.Entities;

/// <summary>
/// Audit trail record for order status transitions.
/// Each row captures a single from → to status change with a timestamp.
/// </summary>
public class OrderStatusHistory
{
    /// <summary>
    /// Primary key stored as a 16-byte binary (GUID).
    /// </summary>
    public required byte[] Id { get; set; }

    /// <summary>
    /// FK – the order whose status changed.
    /// </summary>
    public required byte[] OrderId { get; set; }

    /// <summary>
    /// FK – the status the order was in before the change.
    /// </summary>
    public required byte[] FromStatusId { get; set; }

    /// <summary>
    /// FK – the status the order moved to.
    /// </summary>
    public required byte[] ToStatusId { get; set; }

    /// <summary>
    /// UTC timestamp of the status transition.
    /// </summary>
    public DateTime ChangedAt { get; set; }

    /// <summary>
    /// Navigation – the related order.
    /// </summary>
    public virtual Order Order { get; set; } = null!;

    /// <summary>
    /// Navigation – the previous status.
    /// </summary>
    public virtual OrderStatus FromStatus { get; set; } = null!;

    /// <summary>
    /// Navigation – the new status.
    /// </summary>
    public virtual OrderStatus ToStatus { get; set; } = null!;
}
