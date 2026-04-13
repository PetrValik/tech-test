using System;
using System.Collections.Generic;

namespace Order.Data.Entities;

/// <summary>
/// EF Core entity that maps to the order table.
/// </summary>
public class Order
{
    /// <summary>
    /// Primary key stored as binary(16) in MySQL.
    /// </summary>
    public required byte[] Id { get; set; }

    /// <summary>
    /// FK – the reseller who placed this order.
    /// </summary>
    public required byte[] ResellerId { get; set; }

    /// <summary>
    /// FK – the end-customer this order is for.
    /// </summary>
    public required byte[] CustomerId { get; set; }

    /// <summary>
    /// FK to the current OrderStatus.
    /// </summary>
    public required byte[] StatusId { get; set; }

    /// <summary>
    /// UTC timestamp when the order was created.
    /// </summary>
    public DateTime CreatedDate { get; set; }

    /// <summary>
    /// Optimistic concurrency token. Updated on every status change to detect lost updates.
    /// </summary>
    public string ConcurrencyStamp { get; set; } = Guid.NewGuid().ToString("N");

    /// <summary>
    /// Navigation property to the current status.
    /// </summary>
    public OrderStatus Status { get; set; } = null!;

    /// <summary>
    /// Navigation collection of all line items belonging to this order.
    /// </summary>
    public ICollection<OrderItem> Items { get; set; } = [];
}
