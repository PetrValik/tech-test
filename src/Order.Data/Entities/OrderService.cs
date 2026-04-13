using System.Collections.Generic;

namespace Order.Data.Entities;

/// <summary>
/// EF Core entity that maps to the order_service table.
/// Represents a product category, e.g. "Email" or "Antivirus".
/// </summary>
public class OrderService
{
    /// <summary>
    /// Primary key stored as binary(16) in MySQL.
    /// </summary>
    public required byte[] Id { get; set; }

    /// <summary>
    /// Display name of the service, e.g. "Email".
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Navigation collection of all order items in this service category.
    /// </summary>
    public ICollection<OrderItem> OrderItems { get; set; } = [];

    /// <summary>
    /// Navigation collection of all products that belong to this service.
    /// </summary>
    public ICollection<OrderProduct> OrderProducts { get; set; } = [];
}
