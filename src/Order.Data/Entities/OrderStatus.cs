using System.Collections.Generic;

namespace Order.Data.Entities;

/// <summary>
/// EF Core entity that maps to the order_status table.
/// Represents a lifecycle state such as "Created", "In Progress", "Failed", or "Completed".
/// </summary>
public class OrderStatus
{
    /// <summary>
    /// Primary key stored as binary(16) in MySQL.
    /// </summary>
    public required byte[] Id { get; set; }

    /// <summary>
    /// Status name, e.g. "Completed". Unique, max 20 characters.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Navigation collection of all orders currently in this status.
    /// </summary>
    public ICollection<Order> Orders { get; set; } = [];
}
