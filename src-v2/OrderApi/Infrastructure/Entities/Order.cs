namespace OrderApi.Infrastructure.Entities;

/// <summary>
/// Represents a customer order record in the database.
/// Supports soft-delete via <see cref="IsDeleted"/> and <see cref="DeletedAt"/>.
/// </summary>
public class Order
{
    /// <summary>
    /// Primary key stored as a 16-byte binary (GUID).
    /// </summary>
    public required byte[] Id { get; set; }

    /// <summary>
    /// FK – the reseller who placed this order.
    /// </summary>
    public required byte[] ResellerId { get; set; }

    /// <summary>
    /// FK – the end customer for this order.
    /// </summary>
    public required byte[] CustomerId { get; set; }

    /// <summary>
    /// FK – current status of this order.
    /// </summary>
    public required byte[] StatusId { get; set; }

    /// <summary>
    /// UTC timestamp when the order was created.
    /// </summary>
    public DateTime CreatedDate { get; set; }

    /// <summary>
    /// Indicates whether the order has been soft-deleted.
    /// </summary>
    public bool IsDeleted { get; set; }

    /// <summary>
    /// UTC timestamp when the order was soft-deleted. Null if not deleted.
    /// </summary>
    public DateTime? DeletedAt { get; set; }

    /// <summary>
    /// Optimistic concurrency stamp. Changes on every update.
    /// Returned as an ETag header; clients must send If-Match for safe updates.
    /// </summary>
    public string ConcurrencyStamp { get; set; } = Guid.NewGuid().ToString("N");

    /// <summary>
    /// Navigation property to the order status.
    /// </summary>
    public virtual OrderStatus Status { get; set; } = null!;

    /// <summary>
    /// Navigation property to the order line items.
    /// </summary>
    public virtual ICollection<OrderItem> Items { get; set; } = [];
}
