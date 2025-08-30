namespace OrchestratorChat.Core.Events;

/// <summary>
/// Interface for event bus that handles event publishing and subscription
/// </summary>
public interface IEventBus
{
    /// <summary>
    /// Publish an event to all subscribers
    /// </summary>
    /// <typeparam name="TEvent">Type of event to publish</typeparam>
    /// <param name="event">The event to publish</param>
    /// <returns>Task representing the async operation</returns>
    Task PublishAsync<TEvent>(TEvent @event) where TEvent : IEvent;
    
    /// <summary>
    /// Subscribe to events of a specific type
    /// </summary>
    /// <typeparam name="TEvent">Type of event to subscribe to</typeparam>
    /// <param name="handler">Handler to call when events are received</param>
    void Subscribe<TEvent>(Action<TEvent> handler) where TEvent : IEvent;
    
    /// <summary>
    /// Unsubscribe from events of a specific type
    /// </summary>
    /// <typeparam name="TEvent">Type of event to unsubscribe from</typeparam>
    /// <param name="handler">Handler to remove</param>
    void Unsubscribe<TEvent>(Action<TEvent> handler) where TEvent : IEvent;
    
    /// <summary>
    /// Publish an event to all subscribers synchronously
    /// </summary>
    /// <typeparam name="TEvent">Type of event to publish</typeparam>
    /// <param name="event">The event to publish</param>
    void Publish<TEvent>(TEvent @event) where TEvent : IEvent;
    
    /// <summary>
    /// Subscribe to events of a specific type asynchronously
    /// </summary>
    /// <typeparam name="TEvent">Type of event to subscribe to</typeparam>
    /// <param name="handler">Async handler to call when events are received</param>
    /// <param name="cancellationToken">Cancellation token for the subscription</param>
    /// <returns>A subscription object that can be disposed to unsubscribe</returns>
    Task<IEventSubscription> SubscribeAsync<TEvent>(Func<TEvent, Task> handler, CancellationToken cancellationToken = default) where TEvent : IEvent;
}
