using System;

namespace Order.Model;

/// <summary>
/// A single product line belonging to an order, as returned by the API.
/// </summary>
public class OrderItem
{
    /// <summary>
    /// Unique identifier of this order item.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Identifier of the parent order.
    /// </summary>
    public Guid OrderId { get; set; }

    /// <summary>
    /// Identifier of the service this product belongs to.
    /// </summary>
    public Guid ServiceId { get; set; }

    /// <summary>
    /// Display name of the service, e.g. "Email".
    /// </summary>
    public required string ServiceName { get; set; }

    /// <summary>
    /// Identifier of the product purchased.
    /// </summary>
    public Guid ProductId { get; set; }

    /// <summary>
    /// Display name of the product, e.g. "100GB Mailbox".
    /// </summary>
    public required string ProductName { get; set; }

    /// <summary>
    /// Number of units ordered.
    /// </summary>
    public int Quantity { get; set; }

    /// <summary>
    /// Wholesale cost per unit.
    /// </summary>
    public decimal UnitCost { get; set; }

    /// <summary>
    /// Selling price per unit.
    /// </summary>
    public decimal UnitPrice { get; set; }

    /// <summary>
    /// UnitCost multiplied by Quantity.
    /// </summary>
    public decimal TotalCost { get; set; }

    /// <summary>
    /// UnitPrice multiplied by Quantity.
    /// </summary>
    public decimal TotalPrice { get; set; }
}
