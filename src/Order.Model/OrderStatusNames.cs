namespace Order.Model;

/// <summary>
/// Constants for the four order lifecycle status names stored in the database.
/// Using constants instead of magic strings ensures a single source of truth
/// across validators, services, and tests.
/// </summary>
public static class OrderStatusNames
{
    /// <summary>
    /// Initial status assigned when an order is first created.
    /// </summary>
    public const string Created = "Created";

    /// <summary>
    /// Status indicating the order is currently being processed.
    /// </summary>
    public const string InProgress = "In Progress";

    /// <summary>
    /// Status indicating the order processing failed.
    /// </summary>
    public const string Failed = "Failed";

    /// <summary>
    /// Status indicating the order was successfully fulfilled.
    /// </summary>
    public const string Completed = "Completed";

    /// <summary>
    /// All valid status names as a read-only array, used for whitelist validation.
    /// </summary>
    public static readonly string[] All = { Created, InProgress, Failed, Completed };
}
