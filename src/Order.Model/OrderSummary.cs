using System;

namespace Order.Model;

/// <summary>
/// Lightweight summary of an order, used in list and filter endpoints.
/// </summary>
public class OrderSummary
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
    /// Display name of the current status, e.g. "Created" or "Completed".
    /// </summary>
    public required string StatusName { get; set; }

    /// <summary>
    /// UTC timestamp when the order was created.
    /// </summary>
    public DateTime CreatedDate { get; set; }

    /// <summary>
    /// Total number of line items in this order.
    /// </summary>
    public int ItemCount { get; set; }

    /// <summary>
    /// Sum of UnitCost × Quantity across all items.
    /// </summary>
    public decimal TotalCost { get; set; }

    /// <summary>
    /// Sum of UnitPrice × Quantity across all items.
    /// </summary>
    public decimal TotalPrice { get; set; }
}
