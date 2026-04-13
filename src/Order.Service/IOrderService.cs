using Order.Model;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Order.Service;

/// <summary>
/// Business-logic contract for the Order domain.
/// Sits between the HTTP layer and the data layer, orchestrating validation and repository calls.
/// </summary>
public interface IOrderService
{
    /// <summary>
    /// Returns a summary list of all orders, newest first.
    /// </summary>
    /// <param name="page">1-based page number.</param>
    /// <param name="pageSize">Maximum items per page (capped at 200).</param>
    /// <param name="cancellationToken">Propagates cancellation.</param>
    /// <returns>PagedResult of OrderSummary.</returns>
    Task<PagedResult<OrderSummary>> GetOrdersAsync(int page = 1, int pageSize = 50, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the full detail of a single order including all line items, or null if not found.
    /// </summary>
    /// <param name="orderId">The order's unique identifier.</param>
    /// <param name="cancellationToken">Propagates cancellation.</param>
    /// <returns>OrderDetail or null.</returns>
    Task<OrderDetail?> GetOrderByIdAsync(Guid orderId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns all orders with the specified status name, newest first.
    /// </summary>
    /// <param name="statusName">The exact status name to filter by, e.g. "Failed".</param>
    /// <param name="page">1-based page number.</param>
    /// <param name="pageSize">Maximum items per page (capped at 200).</param>
    /// <param name="cancellationToken">Propagates cancellation.</param>
    /// <returns>PagedResult of OrderSummary.</returns>
    Task<PagedResult<OrderSummary>> GetOrdersByStatusAsync(string statusName, int page = 1, int pageSize = 50, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an order's status and returns the outcome of the operation.
    /// </summary>
    /// <param name="orderId">The unique identifier of the order to update.</param>
    /// <param name="statusName">The target status name.</param>
    /// <param name="cancellationToken">Propagates cancellation.</param>
    /// <returns>UpdateOrderStatusResult indicating success or the reason for failure.</returns>
    Task<UpdateOrderStatusResult> UpdateOrderStatusAsync(Guid orderId, string statusName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new order. Returns a failure result if any supplied product IDs are invalid.
    /// </summary>
    /// <param name="request">The create-order request containing reseller, customer, and item details.</param>
    /// <param name="cancellationToken">Propagates cancellation.</param>
    /// <returns>CreateOrderResult with the new order ID on success, or invalid product IDs on failure.</returns>
    Task<CreateOrderResult> CreateOrderAsync(CreateOrderRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns total profit grouped by calendar month for all Completed orders.
    /// </summary>
    /// <param name="cancellationToken">Propagates cancellation.</param>
    /// <returns>Enumerable of MonthlyProfit, ordered by year and month ascending.</returns>
    Task<IEnumerable<MonthlyProfit>> GetMonthlyProfitAsync(CancellationToken cancellationToken = default);
}
