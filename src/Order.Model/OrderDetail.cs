using System;
using System.Collections.Generic;

namespace Order.Model;

/// <summary>
/// Full detail of an order, including all line items.
/// </summary>
public class OrderDetail
{
    /// <summary>
    /// Unique identifier of the order.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// ID of the reseller who placed the order.
    /// </summary>
    public Guid ResellerId { get; set; }

    /// <summary>
    /// ID of the end-customer this order is for.
    /// </summary>
    public Guid CustomerId { get; set; }

    /// <summary>
    /// ID of the current order status.
    /// </summary>
    public Guid StatusId { get; set; }

    /// <summary>
    /// Display name of the current status, e.g. "In Progress".
    /// </summary>
    public required string StatusName { get; set; }

    /// <summary>
    /// UTC timestamp when the order was created.
    /// </summary>
    public DateTime CreatedDate { get; set; }

    /// <summary>
    /// Sum of UnitCost × Quantity across all items.
    /// </summary>
    public decimal TotalCost { get; set; }

    /// <summary>
    /// Sum of UnitPrice × Quantity across all items.
    /// </summary>
    public decimal TotalPrice { get; set; }

    /// <summary>
    /// All product line items belonging to this order.
    /// </summary>
    public IEnumerable<OrderItem> Items { get; set; } = [];
}
