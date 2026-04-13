namespace OrderApi.Features.Orders.DeleteOrder;

/// <summary>
/// Possible outcomes of a soft-delete operation.
/// </summary>
public enum DeleteOrderResult
{
    /// <summary>
    /// The order was successfully soft-deleted.
    /// </summary>
    Deleted,
    /// <summary>
    /// No order with the given ID was found.
    /// </summary>
    NotFound
}
