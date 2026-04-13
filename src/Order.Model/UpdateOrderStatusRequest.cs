namespace Order.Model;

/// <summary>
/// Request body for the update-order-status endpoint.
/// </summary>
public class UpdateOrderStatusRequest
{
    /// <summary>
    /// Target status name, e.g. "Completed" or "Failed".
    /// </summary>
    public required string StatusName { get; set; }
}
