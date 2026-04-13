namespace OrderApi.Features.Orders.UpdateOrderStatus;

/// <summary>
/// Possible outcomes of an update-order-status operation.
/// </summary>
public enum UpdateResult
{
    /// <summary>
    /// Status updated and saved successfully.
    /// </summary>
    Success,
    /// <summary>
    /// No order with the given ID was found.
    /// </summary>
    OrderNotFound,
    /// <summary>
    /// The requested status name does not exist in the database.
    /// </summary>
    InvalidStatus,
    /// <summary>
    /// The If-Match header does not match the current ConcurrencyStamp.
    /// </summary>
    Conflict
}
