using System;

namespace Order.Model;

/// <summary>
/// A single product line item within a CreateOrderRequest.
/// </summary>
public class CreateOrderItemRequest
{
    /// <summary>
    /// ID of the product to order. Must exist in the product catalogue.
    /// </summary>
    public Guid ProductId { get; set; }

    /// <summary>
    /// Number of units to order. Must be greater than zero.
    /// </summary>
    public int Quantity { get; set; }
}
