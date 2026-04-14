using Microsoft.EntityFrameworkCore;
using OrderApi.Infrastructure;
using OrderApi.Infrastructure.Entities;

namespace OrderApi.Services;

/// <summary>
/// Background job that periodically cancels stale orders — orders that have been
/// in the "Created" status for longer than the configured threshold.
/// Each cancellation writes an audit trail record for traceability.
/// </summary>
/// <param name="serviceProvider">The root service provider used to create per-operation scoped DbContext instances.</param>
/// <param name="configuration">The application configuration used to read interval and stale-days settings.</param>
/// <param name="logger">The logger used to record cleanup activity and errors.</param>
public sealed class StaleOrderCleanupService(
    IServiceProvider serviceProvider,
    IConfiguration configuration,
    ILogger<StaleOrderCleanupService> logger): BackgroundService
{
    /// <inheritdoc/>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var intervalMinutes = configuration.GetValue("StaleOrderCleanup:IntervalMinutes", 60);
        var staleDays = configuration.GetValue("StaleOrderCleanup:StaleDays", 30);

        logger.LogInformation(
            "StaleOrderCleanupService started. Interval: {IntervalMinutes}m, StaleDays: {StaleDays}",
            intervalMinutes, staleDays);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CleanupAsync(staleDays, stoppingToken);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                logger.LogError(exception, "Error during stale order cleanup");
            }

            await Task.Delay(TimeSpan.FromMinutes(intervalMinutes), stoppingToken);
        }
    }

    /// <summary>
    /// Finds and cancels all orders in "Created" status older than <paramref name="staleDays"/> days.
    /// Each order is processed in an isolated DbContext scope so that a concurrency conflict
    /// on one order does not affect the others.
    /// </summary>
    /// <param name="staleDays">Number of days after which a "Created" order is considered stale.</param>
    /// <param name="cancellationToken">Token used to cancel the cleanup operation.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous cleanup operation.</returns>
    internal async Task CleanupAsync(int staleDays, CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OrderContext>();

        var (createdStatus, failedStatus) = await GetRequiredStatusesAsync(db, cancellationToken);

        if (createdStatus is null || failedStatus is null)
        {
            logger.LogWarning("Required statuses not found in database — skipping cleanup");
            return;
        }

        var cutoff = DateTime.UtcNow.AddDays(-staleDays);
        var staleOrderIds = await db.Orders
            .Where(order => order.StatusId == createdStatus.Id && order.CreatedDate < cutoff)
            .Select(order => order.Id)
            .ToListAsync(cancellationToken);

        if (staleOrderIds.Count == 0)
        {
            logger.LogDebug("No stale orders found");
            return;
        }

        int cancelledCount = 0;
        foreach (var orderId in staleOrderIds)
        {
            cancellationToken.ThrowIfCancellationRequested();
            cancelledCount += await TryCancelOrderAsync(orderId, createdStatus.Id, failedStatus.Id, cancellationToken);
        }

        logger.LogInformation(
            "Stale order cleanup complete. Found {StaleCount} stale orders, successfully cancelled {CancelledCount}",
            staleOrderIds.Count, cancelledCount);
    }

    /// <summary>
    /// Loads the "Created" and "Failed" status entities needed for the cleanup run.
    /// Returns (null, null) if either status is missing from the database.
    /// </summary>
    /// <param name="db">The database context to query for order statuses.</param>
    /// <param name="cancellationToken">Token used to cancel the database query.</param>
    /// <returns>
    /// A tuple of the "Created" and "Failed" <see cref="OrderStatus"/> entities,
    /// either of which may be <see langword="null"/> if not found in the database.
    /// </returns>
    /// <remarks>
    /// The second status is "Failed" (the stale order target state).
    /// Variables use <c>failedStatus</c> to match the database value.
    /// </remarks>
    private static async Task<(OrderStatus? created, OrderStatus? failed)> GetRequiredStatusesAsync(
        OrderContext db, CancellationToken cancellationToken)
    {
        var createdStatus = await db.OrderStatuses
            .AsNoTracking()
            .FirstOrDefaultAsync(status => status.Name == OrderStatusNames.Created, cancellationToken);

        var failedStatus = await db.OrderStatuses
            .AsNoTracking()
            .FirstOrDefaultAsync(status => status.Name == OrderStatusNames.Failed, cancellationToken);

        return (createdStatus, failedStatus);
    }

    /// <summary>
    /// Attempts to cancel a single order in its own DbContext scope.
    /// Re-loads the order to verify it is still in "Created" status before saving,
    /// and catches any concurrency conflict without affecting other orders.
    /// </summary>
    /// <param name="orderId">The raw byte-array identifier of the order to cancel.</param>
    /// <param name="createdStatusId">The raw byte-array identifier of the "Created" status.</param>
    /// <param name="failedStatusId">The raw byte-array identifier of the "Failed" status to transition into.</param>
    /// <param name="cancellationToken">Token used to cancel the database operations.</param>
    /// <returns>1 if the order was cancelled, 0 if it was skipped.</returns>
    private async Task<int> TryCancelOrderAsync(
        byte[] orderId, byte[] createdStatusId, byte[] failedStatusId, CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OrderContext>();

        var order = await db.Orders.FirstOrDefaultAsync(order => order.Id == orderId, cancellationToken);

        // Re-verify status against a fresh DB read — another process may have changed it.
        if (order is null || !order.StatusId.SequenceEqual(createdStatusId))
        {
            logger.LogDebug("Order {OrderId} status changed concurrently, skipping", new Guid(orderId));
            return 0;
        }

        db.StatusHistory.Add(new OrderStatusHistory
        {
            Id           = Guid.NewGuid().ToByteArray(),
            OrderId      = orderId,
            FromStatusId = createdStatusId,
            ToStatusId   = failedStatusId,
            ChangedAt    = DateTime.UtcNow
        });

        order.StatusId        = failedStatusId;
        order.ConcurrencyStamp = Guid.NewGuid().ToString("N");

        try
        {
            await db.SaveChangesAsync(cancellationToken);
            return 1;
        }
        catch (DbUpdateConcurrencyException)
        {
            logger.LogDebug(
                "Order {OrderId} was modified between load and save, skipping", new Guid(orderId));
            return 0;
        }
    }
}
