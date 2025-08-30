using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace OrchestratorChat.Core.Events;

/// <summary>
/// Thread-safe event bus implementation for publishing and subscribing to events
/// </summary>
public class EventBus : IEventBus
{
    private readonly ConcurrentDictionary<Type, List<Delegate>> _handlers = new();
    private readonly object _lockObject = new();
    private readonly ILogger<EventBus> _logger;

    /// <summary>
    /// Initializes a new instance of the EventBus
    /// </summary>
    /// <param name="logger">Logger for event bus operations</param>
    public EventBus(ILogger<EventBus> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Publishes an event to all subscribers asynchronously
    /// </summary>
    /// <typeparam name="TEvent">Type of event to publish</typeparam>
    /// <param name="event">The event to publish</param>
    /// <returns>Task representing the async operation</returns>
    public async Task PublishAsync<TEvent>(TEvent @event) where TEvent : IEvent
    {
        if (@event == null)
            throw new ArgumentNullException(nameof(@event));

        var eventType = typeof(TEvent);
        
        if (!_handlers.TryGetValue(eventType, out var handlers) || handlers.Count == 0)
        {
            return;
        }

        List<Delegate> handlersCopy;
        lock (_lockObject)
        {
            handlersCopy = new List<Delegate>(handlers);
        }

        _logger.LogDebug("Publishing event {EventType} with Id {EventId} to {HandlerCount} handlers", 
            eventType.Name, @event.Id, handlersCopy.Count);

        var tasks = handlersCopy.Select(async handler =>
        {
            try
            {
                if (handler is Action<TEvent> action)
                {
                    await Task.Run(() => action(@event));
                }
                else if (handler is Func<TEvent, Task> asyncHandler)
                {
                    await asyncHandler(@event);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing event handler for {EventType} with Id {EventId}", 
                    eventType.Name, @event.Id);
            }
        });

        await Task.WhenAll(tasks);
    }

    /// <summary>
    /// Publishes an event to all subscribers synchronously
    /// </summary>
    /// <typeparam name="TEvent">Type of event to publish</typeparam>
    /// <param name="event">The event to publish</param>
    public void Publish<TEvent>(TEvent @event) where TEvent : IEvent
    {
        if (@event == null)
            throw new ArgumentNullException(nameof(@event));

        var eventType = typeof(TEvent);
        
        if (!_handlers.TryGetValue(eventType, out var handlers) || handlers.Count == 0)
        {
            return;
        }

        List<Delegate> handlersCopy;
        lock (_lockObject)
        {
            handlersCopy = new List<Delegate>(handlers);
        }

        _logger.LogDebug("Publishing event {EventType} with Id {EventId} to {HandlerCount} handlers synchronously", 
            eventType.Name, @event.Id, handlersCopy.Count);

        foreach (var handler in handlersCopy)
        {
            try
            {
                if (handler is Action<TEvent> action)
                {
                    action(@event);
                }
                else if (handler is Func<TEvent, Task> asyncHandler)
                {
                    // For sync publish, we'll run async handlers synchronously
                    asyncHandler(@event).GetAwaiter().GetResult();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing event handler for {EventType} with Id {EventId}", 
                    eventType.Name, @event.Id);
            }
        }
    }

    /// <summary>
    /// Subscribes to events of a specific type
    /// </summary>
    /// <typeparam name="TEvent">Type of event to subscribe to</typeparam>
    /// <param name="handler">Handler to call when events are received</param>
    public void Subscribe<TEvent>(Action<TEvent> handler) where TEvent : IEvent
    {
        if (handler == null)
            throw new ArgumentNullException(nameof(handler));

        var eventType = typeof(TEvent);
        
        lock (_lockObject)
        {
            if (!_handlers.TryGetValue(eventType, out var handlers))
            {
                handlers = new List<Delegate>();
                _handlers[eventType] = handlers;
            }
            
            handlers.Add(handler);
        }

        _logger.LogDebug("Subscribed handler for event type {EventType}. Total handlers: {HandlerCount}", 
            eventType.Name, _handlers[eventType].Count);
    }

    /// <summary>
    /// Unsubscribes from events of a specific type
    /// </summary>
    /// <typeparam name="TEvent">Type of event to unsubscribe from</typeparam>
    /// <param name="handler">Handler to remove</param>
    public void Unsubscribe<TEvent>(Action<TEvent> handler) where TEvent : IEvent
    {
        if (handler == null)
            throw new ArgumentNullException(nameof(handler));

        var eventType = typeof(TEvent);
        
        lock (_lockObject)
        {
            if (_handlers.TryGetValue(eventType, out var handlers))
            {
                var removed = handlers.Remove(handler);
                
                if (removed)
                {
                    _logger.LogDebug("Unsubscribed handler for event type {EventType}. Remaining handlers: {HandlerCount}", 
                        eventType.Name, handlers.Count);
                }
                
                if (handlers.Count == 0)
                {
                    _handlers.TryRemove(eventType, out _);
                    _logger.LogDebug("Removed empty handler list for event type {EventType}", eventType.Name);
                }
            }
        }
    }

    /// <summary>
    /// Subscribe to events of a specific type asynchronously
    /// </summary>
    /// <typeparam name="TEvent">Type of event to subscribe to</typeparam>
    /// <param name="handler">Async handler to call when events are received</param>
    /// <param name="cancellationToken">Cancellation token for the subscription</param>
    /// <returns>A subscription object that can be disposed to unsubscribe</returns>
    public Task<IEventSubscription> SubscribeAsync<TEvent>(Func<TEvent, Task> handler, CancellationToken cancellationToken = default) where TEvent : IEvent
    {
        if (handler == null)
            throw new ArgumentNullException(nameof(handler));

        var eventType = typeof(TEvent);
        
        lock (_lockObject)
        {
            if (!_handlers.TryGetValue(eventType, out var handlers))
            {
                handlers = new List<Delegate>();
                _handlers[eventType] = handlers;
            }
            
            handlers.Add(handler);
        }

        _logger.LogDebug("Subscribed async handler for event type {EventType}. Total handlers: {HandlerCount}", 
            eventType.Name, _handlers[eventType].Count);

        // Return a subscription that can be disposed to unsubscribe
        return Task.FromResult<IEventSubscription>(new EventSubscription(() => UnsubscribeAsync(handler)));
    }

    /// <summary>
    /// Unsubscribes an async handler from events of a specific type
    /// </summary>
    /// <typeparam name="TEvent">Type of event to unsubscribe from</typeparam>
    /// <param name="handler">Async handler to remove</param>
    private void UnsubscribeAsync<TEvent>(Func<TEvent, Task> handler) where TEvent : IEvent
    {
        if (handler == null)
            return;

        var eventType = typeof(TEvent);
        
        lock (_lockObject)
        {
            if (_handlers.TryGetValue(eventType, out var handlers))
            {
                var removed = handlers.Remove(handler);
                
                if (removed)
                {
                    _logger.LogDebug("Unsubscribed async handler for event type {EventType}. Remaining handlers: {HandlerCount}", 
                        eventType.Name, handlers.Count);
                }
                
                if (handlers.Count == 0)
                {
                    _handlers.TryRemove(eventType, out _);
                    _logger.LogDebug("Removed empty handler list for event type {EventType}", eventType.Name);
                }
            }
        }
    }

    /// <summary>
    /// Implementation of event subscription that can be disposed to unsubscribe
    /// </summary>
    private class EventSubscription : IEventSubscription
    {
        private readonly Action _unsubscribeAction;
        private bool _disposed = false;
        
        public bool IsActive => !_disposed;
        
        public EventSubscription(Action unsubscribeAction)
        {
            _unsubscribeAction = unsubscribeAction ?? throw new ArgumentNullException(nameof(unsubscribeAction));
        }
        
        public void Dispose()
        {
            if (!_disposed)
            {
                _unsubscribeAction();
                _disposed = true;
            }
        }
    }
}