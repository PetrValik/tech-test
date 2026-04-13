namespace OrderApi.Infrastructure.Entities;

/// <summary>
/// A top-level service category that groups related products (e.g., Email, Antivirus).
/// </summary>
public class OrderService
{
    /// <summary>
    /// Primary key stored as a 16-byte binary (GUID).
    /// </summary>
    public required byte[] Id { get; set; }

    /// <summary>
    /// Human-readable service name (e.g., "Email").
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Navigation property to products in this service category.
    /// </summary>
    public virtual ICollection<OrderProduct> OrderProducts { get; set; } = [];

    /// <summary>
    /// Navigation property to order items linked to this service.
    /// </summary>
    public virtual ICollection<OrderItem> OrderItems { get; set; } = [];
}
