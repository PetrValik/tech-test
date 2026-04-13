namespace Order.Data.Entities;

/// <summary>
/// EF Core entity that maps to the order_item table.
/// </summary>
public class OrderItem
{
    /// <summary>
    /// Primary key stored as binary(16) in MySQL.
    /// </summary>
    public required byte[] Id { get; set; }

    /// <summary>
    /// FK to the parent Order.
    /// </summary>
    public required byte[] OrderId { get; set; }

    /// <summary>
    /// FK to the OrderProduct that was purchased.
    /// </summary>
    public required byte[] ProductId { get; set; }

    /// <summary>
    /// FK to the OrderService category.
    /// </summary>
    public required byte[] ServiceId { get; set; }

    /// <summary>
    /// Number of units ordered.
    /// </summary>
    public int Quantity { get; set; }

    /// <summary>
    /// Navigation property to the parent order.
    /// </summary>
    public Order Order { get; set; } = null!;

    /// <summary>
    /// Navigation property to the purchased product.
    /// </summary>
    public OrderProduct Product { get; set; } = null!;

    /// <summary>
    /// Navigation property to the service category.
    /// </summary>
    public OrderService Service { get; set; } = null!;
}
