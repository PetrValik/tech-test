using Microsoft.Extensions.Logging;

namespace OrderApi.Common.Events;

/// <summary>
/// No-op event publisher for local development and testing.
/// Logs a debug message instead of sending to a real message broker.
/// Replace with a real implementation in production environments.
/// </summary>
internal sealed class NullOrderEventPublisher(ILogger<NullOrderEventPublisher> logger)
    : IOrderEventPublisher
{
    /// <inheritdoc/>
    public Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
        where TEvent : class
    {
        logger.LogDebug(
            "[NullOrderEventPublisher] Event published (no broker configured): {EventType} {@Event}",
            typeof(TEvent).Name, @event);
        return Task.CompletedTask;
    }
}
