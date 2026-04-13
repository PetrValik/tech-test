using Order.Model;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Order.Data.Repositories;

/// <summary>
/// Data-access contract for the Order aggregate.
/// All methods are async and accept an optional CancellationToken.
/// </summary>
public interface IOrderRepository
{
    /// <summary>
    /// Returns a summary list of all orders, newest first.
    /// </summary>
    /// <param name="page">1-based page number.</param>
    /// <param name="pageSize">Maximum items per page.</param>
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
    /// Returns all orders whose status name matches <paramref name="statusName"/>, newest first.
    /// </summary>
    /// <param name="statusName">The exact status name to filter by, e.g. "Completed".</param>
    /// <param name="page">1-based page number.</param>
    /// <param name="pageSize">Maximum items per page.</param>
    /// <param name="cancellationToken">Propagates cancellation.</param>
    /// <returns>PagedResult of OrderSummary.</returns>
    Task<PagedResult<OrderSummary>> GetOrdersByStatusAsync(string statusName, int page = 1, int pageSize = 50, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns total profit grouped by calendar month for all Completed orders.
    /// </summary>
    /// <param name="cancellationToken">Propagates cancellation.</param>
    /// <returns>Enumerable of MonthlyProfit, ordered by year and month ascending.</returns>
    Task<IEnumerable<MonthlyProfit>> GetMonthlyProfitAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds an OrderStatus entity by its name, or null if not found.
    /// </summary>
    /// <param name="name">The status name to look up, e.g. "Created".</param>
    /// <param name="cancellationToken">Propagates cancellation.</param>
    /// <returns>The matched entity or null.</returns>
    Task<Entities.OrderStatus?> FindStatusByNameAsync(string name, CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds a tracked Order entity by ID, or null if not found.
    /// Returned entity is change-tracked so mutations are saved by UpdateOrderAsync.
    /// </summary>
    /// <param name="orderId">The order's unique identifier.</param>
    /// <param name="cancellationToken">Propagates cancellation.</param>
    /// <returns>The matched entity or null.</returns>
    Task<Entities.Order?> FindOrderByIdAsync(Guid orderId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns all OrderProduct entities whose IDs appear in <paramref name="productIds"/>.
    /// </summary>
    /// <param name="productIds">The product IDs to search for.</param>
    /// <param name="cancellationToken">Propagates cancellation.</param>
    /// <returns>List of matched product entities.</returns>
    Task<List<Entities.OrderProduct>> FindProductsByIdsAsync(IEnumerable<Guid> productIds, CancellationToken cancellationToken = default);

    /// <summary>
    /// Persists a new order and its line items as a single atomic write.
    /// </summary>
    /// <param name="order">The order entity to insert.</param>
    /// <param name="items">The line-item entities to insert alongside the order.</param>
    /// <param name="cancellationToken">Propagates cancellation.</param>
    Task SaveOrderAsync(Entities.Order order, IEnumerable<Entities.OrderItem> items, CancellationToken cancellationToken = default);

    /// <summary>
    /// Flushes pending change-tracked mutations (e.g. a status update) to the database.
    /// </summary>
    /// <param name="cancellationToken">Propagates cancellation.</param>
    Task UpdateOrderAsync(CancellationToken cancellationToken = default);
}
