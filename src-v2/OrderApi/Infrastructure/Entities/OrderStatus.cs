namespace OrderApi.Infrastructure.Entities;

/// <summary>
/// A valid order lifecycle status (e.g., Created, In Progress, Failed, Completed).
/// </summary>
public class OrderStatus
{
    /// <summary>
    /// Primary key stored as a 16-byte binary (GUID).
    /// </summary>
    public required byte[] Id { get; set; }

    /// <summary>
    /// Display name of the status (e.g., "Completed").
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Navigation property to all orders with this status.
    /// </summary>
    public virtual ICollection<Order> Orders { get; set; } = [];
}
