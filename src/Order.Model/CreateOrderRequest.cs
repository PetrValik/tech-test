using System;
using System.Collections.Generic;

namespace Order.Model;

/// <summary>
/// Request body for creating a new order.
/// </summary>
public class CreateOrderRequest
{
    /// <summary>
    /// ID of the reseller placing the order.
    /// </summary>
    public Guid ResellerId { get; set; }

    /// <summary>
    /// ID of the end-customer this order is for.
    /// </summary>
    public Guid CustomerId { get; set; }

    /// <summary>
    /// One or more product line items to include in the order.
    /// </summary>
    public IReadOnlyList<CreateOrderItemRequest> Items { get; set; } = [];
}
