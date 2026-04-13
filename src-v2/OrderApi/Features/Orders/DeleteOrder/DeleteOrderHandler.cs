using MediatR;
using Microsoft.EntityFrameworkCore;
using OrderApi.Common.Events;
using OrderApi.Infrastructure;

namespace OrderApi.Features.Orders.DeleteOrder;

/// <summary>
/// Sets IsDeleted = true and DeletedAt = UtcNow on the order
/// without physically removing it from the database (soft-delete pattern).
/// </summary>
/// <param name="orderContext">The database context used to load and update the order.</param>
/// <param name="eventPublisher">The event publisher used to dispatch the order-deleted event.</param>
public sealed class DeleteOrderHandler(OrderContext orderContext, IOrderEventPublisher eventPublisher): IRequestHandler<DeleteOrderCommand, DeleteOrderResult>
{
    /// <summary>
    /// Soft-deletes the order by setting IsDeleted = true and DeletedAt = UtcNow,
    /// then publishes an <see cref="OrderApi.Common.Events.OrderDeletedEvent"/>.
    /// Returns <see cref="DeleteOrderResult.NotFound"/> if the order does not exist.
    /// </summary>
    /// <param name="request">The command containing the ID of the order to soft-delete.</param>
    /// <param name="cancellationToken">Token used to cancel the database operations.</param>
    /// <returns>A <see cref="Task{TResult}"/> containing a <see cref="DeleteOrderResult"/> indicating the outcome of the operation.</returns>
    public async Task<DeleteOrderResult> Handle(DeleteOrderCommand request, CancellationToken cancellationToken)
    {
        var idBytes = request.OrderId.ToByteArray();
        var order = await orderContext.Orders
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(order => order.Id == idBytes, cancellationToken);

        if (order is null)
        {
            return DeleteOrderResult.NotFound;
        }
        if (order.IsDeleted)
        {
            return DeleteOrderResult.Deleted; // already deleted — idempotent
        }

        var deletedAt = DateTime.UtcNow;
        order.IsDeleted = true;
        order.DeletedAt = deletedAt;
        await orderContext.SaveChangesAsync(cancellationToken);

        await eventPublisher.PublishAsync(new OrderDeletedEvent(
            OrderId: request.OrderId,
            DeletedAt: deletedAt), cancellationToken);

        return DeleteOrderResult.Deleted;
    }
}
