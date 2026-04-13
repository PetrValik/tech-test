namespace OrderApi.Common.Events;

/// <summary>
/// Publishes domain events when significant order state changes occur.
/// Implementations route events to the configured message broker
/// (Azure Service Bus, AWS SQS/SNS, RabbitMQ, etc.).
/// </summary>
/// <remarks>
/// The null implementation (<see cref="NullOrderEventPublisher"/>) is registered by default
/// so the application runs without a message broker in development.
/// Swap it for a real publisher in production by registering a different implementation
/// in the DI container.
/// </remarks>
public interface IOrderEventPublisher
{
    /// <summary>
    /// Publishes an event asynchronously to the message broker.
    /// Failures should be logged but must not roll back the originating transaction.
    /// </summary>
    /// <typeparam name="TEvent">The domain event type.</typeparam>
    /// <param name="event">The event payload to publish.</param>
    /// <param name="cancellationToken">Propagates cancellation.</param>
    Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
        where TEvent : class;
}
