namespace OrderApi.Infrastructure.Entities;

/// <summary>
/// A purchasable product within a service category, including pricing information.
/// </summary>
public class OrderProduct
{
    /// <summary>
    /// Primary key stored as a 16-byte binary (GUID).
    /// </summary>
    public required byte[] Id { get; set; }

    /// <summary>
    /// FK – the service category this product belongs to.
    /// </summary>
    public required byte[] ServiceId { get; set; }

    /// <summary>
    /// Human-readable product name.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Wholesale cost per unit (used to calculate profit margin).
    /// </summary>
    public decimal UnitCost { get; set; }

    /// <summary>
    /// Retail price per unit charged to the reseller.
    /// </summary>
    public decimal UnitPrice { get; set; }

    /// <summary>
    /// Navigation property to the owning service.
    /// </summary>
    public virtual OrderService Service { get; set; } = null!;

    /// <summary>
    /// Navigation property to all order items referencing this product.
    /// </summary>
    public virtual ICollection<OrderItem> OrderItems { get; set; } = [];
}
