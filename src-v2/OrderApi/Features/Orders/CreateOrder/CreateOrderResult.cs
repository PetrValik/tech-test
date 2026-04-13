namespace OrderApi.Features.Orders.CreateOrder;

/// <summary>
/// Result returned after attempting to create an order.
/// </summary>
/// <param name="OrderId">The new order's GUID when creation succeeds; null on failure.</param>
/// <param name="InvalidProductIds">Product IDs that could not be found; empty on success.</param>
public record CreateOrderResult(Guid? OrderId, IReadOnlyList<Guid> InvalidProductIds)
{
    /// <summary>
    /// True when the order was created successfully (no invalid products).
    /// </summary>
    public bool Success => InvalidProductIds.Count == 0;

    /// <summary>
    /// Creates a successful result with the new order's ID.
    /// </summary>
    /// <param name="orderId">The newly created order's GUID.</param>
    /// <returns>A successful CreateOrderResult.</returns>
    public static CreateOrderResult Ok(Guid orderId) =>
        new(orderId, []);

    /// <summary>
    /// Creates a failure result listing the product IDs that were not found.
    /// </summary>
    /// <param name="invalidIds">Product IDs that do not exist in the database.</param>
    /// <returns>A failed CreateOrderResult.</returns>
    public static CreateOrderResult Invalid(IReadOnlyList<Guid> invalidIds) =>
        new(null, invalidIds);
}
