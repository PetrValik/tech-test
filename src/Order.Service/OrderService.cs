using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Order.Data;
using Order.Data.Repositories;
using Order.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Order.Service;

/// <summary>
/// Default implementation of IOrderService.
/// Delegates persistence to IOrderRepository and contains any cross-cutting business logic.
/// </summary>
public class OrderService : IOrderService
{
    /// <summary>
    /// Repository for data access, injected via constructor and abstracted by interface for testability.
    /// </summary>
    private readonly IOrderRepository _orderRepository;

    /// <summary>
    /// Provides logging capabilities for the OrderService.
    /// </summary>
    private readonly ILogger<OrderService> _logger;

    /// <summary>
    /// Creates a new service instance backed by the supplied repository.
    /// </summary>
    /// <param name="orderRepository">The data-access implementation injected by the DI container.</param>
    /// <param name="logger">Logger for structured diagnostic output.</param>
    public OrderService(IOrderRepository orderRepository, ILogger<OrderService> logger)
    {
        _orderRepository = orderRepository;
        _logger = logger;
    }

    /// <inheritdoc/>
    public Task<PagedResult<OrderSummary>> GetOrdersAsync(int page = 1, int pageSize = 50, CancellationToken cancellationToken = default)
    {
        return _orderRepository.GetOrdersAsync(page, pageSize, cancellationToken);
    }

    /// <inheritdoc/>
    public Task<OrderDetail?> GetOrderByIdAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        return _orderRepository.GetOrderByIdAsync(orderId, cancellationToken);
    }

    /// <inheritdoc/>
    public Task<PagedResult<OrderSummary>> GetOrdersByStatusAsync(string statusName, int page = 1, int pageSize = 50, CancellationToken cancellationToken = default)
    {
        return _orderRepository.GetOrdersByStatusAsync(statusName, page, pageSize, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<UpdateOrderStatusResult> UpdateOrderStatusAsync(Guid orderId, string statusName, CancellationToken cancellationToken = default)
    {
        var status = await _orderRepository.FindStatusByNameAsync(statusName, cancellationToken);
        if (status == null)
        {
            return UpdateOrderStatusResult.InvalidStatus;
        }

        var order = await _orderRepository.FindOrderByIdAsync(orderId, cancellationToken);
        if (order == null)
        {
            return UpdateOrderStatusResult.OrderNotFound;
        }

        var originalStamp = order.ConcurrencyStamp;
        var originalStatusId = order.StatusId;
        order.StatusId = status.Id;
        order.ConcurrencyStamp = Guid.NewGuid().ToString("N");

        try
        {
            await _orderRepository.UpdateOrderAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            order.ConcurrencyStamp = originalStamp;
            order.StatusId = originalStatusId;
            return UpdateOrderStatusResult.ConcurrencyConflict;
        }

        _logger.LogInformation("Order {OrderId} status updated to '{StatusName}'", orderId, statusName);
        return UpdateOrderStatusResult.Success;
    }

    /// <inheritdoc/>
    public async Task<CreateOrderResult> CreateOrderAsync(CreateOrderRequest request, CancellationToken cancellationToken = default)
    {
        var requestedProductIds = request.Items.Select(item => item.ProductId).ToList();
        var products = await _orderRepository.FindProductsByIdsAsync(requestedProductIds, cancellationToken);

        var invalidProductIds = FindInvalidProductIds(request.Items, products);
        if (invalidProductIds.Count > 0)
        {
            return new CreateOrderResult { InvalidProductIds = invalidProductIds };
        }

        var createdStatus = await _orderRepository.FindStatusByNameAsync(OrderStatusNames.Created, cancellationToken)
            ?? throw new InvalidOperationException($"Required status '{OrderStatusNames.Created}' not found in database.");

        var orderId = Guid.NewGuid();
        var orderEntity = BuildOrderEntity(orderId, request, createdStatus);
        var orderItems = BuildOrderItems(orderId, request, products);

        await _orderRepository.SaveOrderAsync(orderEntity, orderItems, cancellationToken);
        _logger.LogInformation("Order {OrderId} created with {ItemCount} item(s)", orderId, orderItems.Count);
        return new CreateOrderResult { OrderId = orderId };
    }

    /// <summary>
    /// Identifies which product IDs in the request were not found in the product catalogue.
    /// </summary>
    /// <param name="requestItems">Line items from the create-order request.</param>
    /// <param name="foundProducts">Products retrieved from the repository for the requested IDs.</param>
    /// <returns>A list of product IDs that had no matching catalogue entry; empty when all IDs are valid.</returns>
    private static List<Guid> FindInvalidProductIds(
        IReadOnlyList<CreateOrderItemRequest> requestItems,
        List<Data.Entities.OrderProduct> foundProducts)
    {
        var foundProductIds = foundProducts.Select(product => new Guid(product.Id)).ToHashSet();
        return requestItems
            .Where(item => !foundProductIds.Contains(item.ProductId))
            .Select(item => item.ProductId)
            .ToList();
    }

    /// <summary>
    /// Constructs the <see cref="Data.Entities.Order"/> entity to be persisted.
    /// </summary>
    /// <param name="orderId">The newly generated order identifier.</param>
    /// <param name="request">The validated create-order request.</param>
    /// <param name="createdStatus">The <c>Created</c> status entity used to initialise the order.</param>
    /// <returns>A new, unpersisted order entity.</returns>
    private static Data.Entities.Order BuildOrderEntity(
        Guid orderId,
        CreateOrderRequest request,
        Data.Entities.OrderStatus createdStatus)
    {
        return new Data.Entities.Order
        {
            Id = orderId.ToByteArray(),
            ResellerId = request.ResellerId.ToByteArray(),
            CustomerId = request.CustomerId.ToByteArray(),
            StatusId = createdStatus.Id,
            CreatedDate = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Constructs the list of <see cref="Data.Entities.OrderItem"/> entities for each requested line item.
    /// </summary>
    /// <param name="orderId">The newly generated order identifier that each item references.</param>
    /// <param name="request">The validated create-order request containing quantity and product selections.</param>
    /// <param name="products">The catalogue products that were resolved from the request's product IDs.</param>
    /// <returns>A list of unpersisted order-item entities ready to be saved alongside the order.</returns>
    private static List<Data.Entities.OrderItem> BuildOrderItems(
        Guid orderId,
        CreateOrderRequest request,
        List<Data.Entities.OrderProduct> products)
    {
        return request.Items.Select(item =>
        {
            var product = products.First(catalogProduct => new Guid(catalogProduct.Id) == item.ProductId);
            return new Data.Entities.OrderItem
            {
                Id = Guid.NewGuid().ToByteArray(),
                OrderId = orderId.ToByteArray(),
                ProductId = product.Id,
                ServiceId = product.ServiceId,
                Quantity = item.Quantity
            };
        }).ToList();
    }

    /// <inheritdoc/>
    public Task<IEnumerable<MonthlyProfit>> GetMonthlyProfitAsync(CancellationToken cancellationToken = default)
    {
        return _orderRepository.GetMonthlyProfitAsync(cancellationToken);
    }
}
