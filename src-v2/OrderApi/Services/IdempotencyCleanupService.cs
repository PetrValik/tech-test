using Microsoft.EntityFrameworkCore;
using OrderApi.Infrastructure;

namespace OrderApi.Services;

/// <summary>
/// Background job that periodically deletes expired idempotency records.
/// Records older than the configured retention period are no longer needed —
/// duplicate requests that arrive after the retention window are treated as new requests.
/// </summary>
/// <param name="serviceProvider">The root service provider used to create per-operation scoped DbContext instances.</param>
/// <param name="configuration">The application configuration used to read interval and retention-days settings.</param>
/// <param name="logger">The logger used to record cleanup activity and errors.</param>
public sealed class IdempotencyCleanupService(
    IServiceProvider serviceProvider,
    IConfiguration configuration,
    ILogger<IdempotencyCleanupService> logger): BackgroundService
{
    /// <inheritdoc/>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var intervalMinutes = configuration.GetValue("IdempotencyCleanup:IntervalMinutes", 60);
        var retentionDays = configuration.GetValue("IdempotencyCleanup:RetentionDays", 7);

        logger.LogInformation(
            "IdempotencyCleanupService started. Interval: {IntervalMinutes}m, RetentionDays: {RetentionDays}",
            intervalMinutes, retentionDays);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CleanupAsync(retentionDays, stoppingToken);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                logger.LogError(exception, "Error during idempotency record cleanup");
            }

            await Task.Delay(TimeSpan.FromMinutes(intervalMinutes), stoppingToken);
        }
    }

    /// <summary>
    /// Deletes all idempotency records older than <paramref name="retentionDays"/> days.
    /// </summary>
    /// <param name="retentionDays">Number of days to retain idempotency records before deleting them.</param>
    /// <param name="cancellationToken">Token used to cancel the cleanup operation.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous cleanup operation.</returns>
    public async Task CleanupAsync(int retentionDays, CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OrderContext>();

        var cutoff = DateTime.UtcNow.AddDays(-retentionDays);

        var deleted = await db.IdempotencyRecords
            .Where(record => record.CreatedAt < cutoff)
            .ExecuteDeleteAsync(cancellationToken);

        if (deleted > 0)
        {
            logger.LogInformation(
                "Idempotency cleanup complete. Deleted {DeletedCount} expired records (older than {RetentionDays} days)",
                deleted, retentionDays);
        }
        else
        {
            logger.LogDebug("No expired idempotency records found");
        }
    }
}
