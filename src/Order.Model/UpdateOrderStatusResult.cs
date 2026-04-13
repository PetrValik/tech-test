namespace Order.Model;

/// <summary>
/// Represents the outcome of an update-order-status operation.
/// </summary>
public enum UpdateOrderStatusResult
{
    /// <summary>
    /// The status was updated successfully.
    /// </summary>
    Success,

    /// <summary>
    /// No order with the supplied ID was found.
    /// </summary>
    OrderNotFound,

    /// <summary>
    /// The supplied status name does not exist in the database.
    /// </summary>
    InvalidStatus,

    /// <summary>
    /// Another request modified the order concurrently (optimistic concurrency conflict).
    /// </summary>
    ConcurrencyConflict
}
