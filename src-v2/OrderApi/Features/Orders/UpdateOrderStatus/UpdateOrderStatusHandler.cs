using MediatR;
using Microsoft.EntityFrameworkCore;
using OrderApi.Common.Events;
using OrderApi.Infrastructure;
using OrderApi.Infrastructure.Entities;

namespace OrderApi.Features.Orders.UpdateOrderStatus;

/// <summary>
/// Handles updating the status of an existing order.
/// Records an audit trail entry for the status transition.
/// </summary>
/// <param name="orderContext">The database context used to load the order and persist the status change.</param>
/// <param name="eventPublisher">The event publisher used to dispatch the status-changed event.</param>
public sealed class UpdateOrderStatusHandler(OrderContext orderContext, IOrderEventPublisher eventPublisher)
    : IRequestHandler<UpdateOrderStatusCommand, UpdateResult>
{
    /// <summary>
    /// Looks up the target status and the order, records the transition in the
    /// audit trail, then persists the change.
    /// </summary>
    /// <param name="command">The command containing the order ID, the target status name, and an optional ETag for optimistic concurrency.</param>
    /// <param name="cancellationToken">Token used to cancel the database operations.</param>
    /// <returns>A <see cref="Task{TResult}"/> containing an <see cref="UpdateResult"/> indicating the outcome of the operation.</returns>
    public async Task<UpdateResult> Handle(UpdateOrderStatusCommand command, CancellationToken cancellationToken)
    {
        var canonicalName = OrderStatusNames.All.FirstOrDefault(
            statusName => string.Equals(statusName, command.StatusName, StringComparison.OrdinalIgnoreCase));
        if (canonicalName is null)
        {
            return UpdateResult.InvalidStatus;
        }

        var status = await orderContext.OrderStatuses.FirstOrDefaultAsync(status => status.Name == canonicalName, cancellationToken);
        if (status is null)
        {
            return UpdateResult.InvalidStatus;
        }

        var orderIdBytes = command.OrderId.ToByteArray();
        var order = await orderContext.Orders.FirstOrDefaultAsync(order => order.Id == orderIdBytes, cancellationToken);
        if (order is null)
        {
            return UpdateResult.OrderNotFound;
        }

        if (order.StatusId.SequenceEqual(status.Id))
        {
            return UpdateResult.Success; // already in the requested status, no-op
        }

        if (command.IfMatch is not null && command.IfMatch != order.ConcurrencyStamp)
        {
            return UpdateResult.Conflict;
        }

        await using var transaction = await orderContext.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            orderContext.StatusHistory.Add(BuildStatusHistoryEntry(order.Id, order.StatusId, status.Id));

            order.StatusId = status.Id;
            order.ConcurrencyStamp = Guid.NewGuid().ToString("N");

            await orderContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            // Transaction is rolled back automatically when the await using block disposes.
            return UpdateResult.Conflict;
        }

        await eventPublisher.PublishAsync(new OrderStatusChangedEvent(
            OrderId: command.OrderId,
            NewStatus: canonicalName,
            OccurredAt: DateTimeOffset.UtcNow), cancellationToken);

        return UpdateResult.Success;
    }

    /// <summary>
    /// Creates an <see cref="OrderStatusHistory"/> audit entry for the given status transition.
    /// </summary>
    /// <param name="orderId">The raw byte-array identifier of the order being transitioned.</param>
    /// <param name="fromStatusId">The raw byte-array identifier of the order's current status.</param>
    /// <param name="toStatusId">The raw byte-array identifier of the new status being applied.</param>
    /// <returns>A new <see cref="OrderStatusHistory"/> entity ready to be added to the database context.</returns>
    private static OrderStatusHistory BuildStatusHistoryEntry(byte[] orderId, byte[] fromStatusId, byte[] toStatusId) =>
        new OrderStatusHistory
        {
            Id = Guid.NewGuid().ToByteArray(),
            OrderId = orderId,
            FromStatusId = fromStatusId,
            ToStatusId = toStatusId,
            ChangedAt = DateTime.UtcNow
        };
}
