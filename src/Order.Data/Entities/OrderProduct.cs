using System.Collections.Generic;

namespace Order.Data.Entities;

/// <summary>
/// EF Core entity that maps to the order_product table.
/// </summary>
public class OrderProduct
{
    /// <summary>
    /// Primary key stored as binary(16) in MySQL.
    /// </summary>
    public required byte[] Id { get; set; }

    /// <summary>
    /// FK to the OrderService this product belongs to.
    /// </summary>
    public required byte[] ServiceId { get; set; }

    /// <summary>
    /// Display name of the product, e.g. "100GB Mailbox".
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Wholesale cost per unit.
    /// </summary>
    public decimal UnitCost { get; set; }

    /// <summary>
    /// Selling price per unit.
    /// </summary>
    public decimal UnitPrice { get; set; }

    /// <summary>
    /// Navigation property to the owning service.
    /// </summary>
    public OrderService Service { get; set; } = null!;

    /// <summary>
    /// Navigation collection of all order items that reference this product.
    /// </summary>
    public ICollection<OrderItem> OrderItems { get; set; } = [];
}
