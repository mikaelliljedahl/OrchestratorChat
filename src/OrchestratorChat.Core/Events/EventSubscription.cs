namespace OrchestratorChat.Core.Events;

/// <summary>
/// Represents a concrete subscription to an event that can be disposed to unsubscribe
/// </summary>
internal class EventSubscription : IEventSubscription
{
    private readonly Action _unsubscribeAction;
    private bool _disposed = false;

    /// <summary>
    /// Initializes a new instance of the EventSubscription class
    /// </summary>
    /// <param name="unsubscribeAction">Action to call when disposing to unsubscribe</param>
    public EventSubscription(Action unsubscribeAction)
    {
        _unsubscribeAction = unsubscribeAction ?? throw new ArgumentNullException(nameof(unsubscribeAction));
    }

    /// <summary>
    /// Gets a value indicating whether this subscription is still active
    /// </summary>
    public bool IsActive => !_disposed;

    /// <summary>
    /// Disposes the subscription, effectively unsubscribing from the event
    /// </summary>
    public void Dispose()
    {
        if (!_disposed)
        {
            _unsubscribeAction?.Invoke();
            _disposed = true;
        }
    }
}