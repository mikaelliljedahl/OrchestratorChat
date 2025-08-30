namespace OrchestratorChat.Core.Events;

/// <summary>
/// Represents a subscription to an event that can be disposed to unsubscribe
/// </summary>
public interface IEventSubscription : IDisposable
{
    /// <summary>
    /// Gets a value indicating whether this subscription is still active
    /// </summary>
    bool IsActive { get; }
}