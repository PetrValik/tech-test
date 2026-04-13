using MediatR;
using Microsoft.EntityFrameworkCore;
using OrderApi.Common.Events;
using OrderApi.Infrastructure;
using OrderApi.Infrastructure.Entities;

namespace OrderApi.Features.Orders.CreateOrder;

/// <summary>
/// Handles order creation: validates product existence, builds entities, and persists them.
/// </summary>
/// <param name="orderContext">The database context used to read products and persist orders.</param>
/// <param name="eventPublisher">The event publisher used to dispatch the order-created event.</param>
public sealed class CreateOrderHandler(OrderContext orderContext, IOrderEventPublisher eventPublisher)
    : IRequestHandler<CreateOrderCommand, CreateOrderResult>
{
    /// <summary>
    /// Creates a new order from the given command.
    /// Returns a failure result if any product IDs are not found in the database.
    /// </summary>
    /// <param name="command">The command containing reseller ID, customer ID, and the list of items to order.</param>
    /// <param name="cancellationToken">Token used to cancel the database operations.</param>
    /// <returns>A <see cref="Task{TResult}"/> containing a <see cref="CreateOrderResult"/> indicating success or the reason for failure.</returns>
    public async Task<CreateOrderResult> Handle(CreateOrderCommand command, CancellationToken cancellationToken)
    {
        var requestedIds = command.Items.Select(item => item.ProductId.ToByteArray()).ToList();

        var products = await orderContext.OrderProducts
            .AsNoTracking()
            .Where(product => requestedIds.Contains(product.Id))
            .ToListAsync(cancellationToken);

        var productMap = products.ToDictionary(product => new Guid(product.Id));

        var invalidIds = command.Items
            .Where(item => !productMap.ContainsKey(item.ProductId))
            .Select(item => item.ProductId)
            .ToList();

        if (invalidIds.Count > 0)
        {
            return CreateOrderResult.Invalid(invalidIds);
        }

        // A missing 'Created' status is a configuration error, not a user error — throw to return 500.
        var createdStatus = await orderContext.OrderStatuses
            .FirstOrDefaultAsync(status => status.Name == OrderStatusNames.Created, cancellationToken)
            ?? throw new InvalidOperationException("Required status 'Created' not found in the database.");

        var orderId = Guid.NewGuid();

        var orderItems = command.Items.Select(item =>
        {
            var product = productMap[item.ProductId];
            return new OrderItem
            {
                Id = Guid.NewGuid().ToByteArray(),
                OrderId = orderId.ToByteArray(),
                ProductId = product.Id,
                ServiceId = product.ServiceId,
                Quantity = item.Quantity
            };
        }).ToList();

        orderContext.Orders.Add(new Infrastructure.Entities.Order
        {
            Id = orderId.ToByteArray(),
            ResellerId = command.ResellerId.ToByteArray(),
            CustomerId = command.CustomerId.ToByteArray(),
            StatusId = createdStatus.Id,
            CreatedDate = DateTime.UtcNow
        });

        orderContext.OrderItems.AddRange(orderItems);
        await orderContext.SaveChangesAsync(cancellationToken);

        await eventPublisher.PublishAsync(new OrderCreatedEvent(
            OrderId: orderId,
            ResellerId: command.ResellerId,
            CustomerId: command.CustomerId,
            OccurredAt: DateTimeOffset.UtcNow), cancellationToken);

        return CreateOrderResult.Ok(orderId);
    }
}
