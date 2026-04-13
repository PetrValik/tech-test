using System;
using System.Collections.Generic;

namespace Order.Model;

/// <summary>
/// Result of a CreateOrderAsync operation.
/// </summary>
public class CreateOrderResult
{
    /// <summary>
    /// ID of the newly created order. Null when the operation failed.
    /// </summary>
    public Guid? OrderId { get; set; }

    /// <summary>
    /// Product IDs from the request that were not found in the catalogue.
    /// </summary>
    public IReadOnlyList<Guid> InvalidProductIds { get; set; } = [];

    /// <summary>
    /// True when no invalid product IDs were encountered and the order was persisted.
    /// </summary>
    public bool Success => InvalidProductIds.Count == 0;
}
