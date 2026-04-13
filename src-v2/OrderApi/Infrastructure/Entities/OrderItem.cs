namespace OrderApi.Infrastructure.Entities;

/// <summary>
/// A single line item within an order, linking a product and its quantity.
/// </summary>
public class OrderItem
{
    /// <summary>
    /// Primary key stored as a 16-byte binary (GUID).
    /// </summary>
    public required byte[] Id { get; set; }

    /// <summary>
    /// FK – the order this item belongs to.
    /// </summary>
    public required byte[] OrderId { get; set; }

    /// <summary>
    /// FK – the product being ordered.
    /// </summary>
    public required byte[] ProductId { get; set; }

    /// <summary>
    /// FK – the service category the product belongs to.
    /// </summary>
    public required byte[] ServiceId { get; set; }

    /// <summary>
    /// Number of units ordered; nullable to match the database column definition.
    /// </summary>
    public int? Quantity { get; set; }

    /// <summary>
    /// Navigation property to the parent order.
    /// </summary>
    public virtual Order Order { get; set; } = null!;

    /// <summary>
    /// Navigation property to the ordered product.
    /// </summary>
    public virtual OrderProduct Product { get; set; } = null!;

    /// <summary>
    /// Navigation property to the service category.
    /// </summary>
    public virtual OrderService Service { get; set; } = null!;
}
