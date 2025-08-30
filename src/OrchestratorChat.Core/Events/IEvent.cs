namespace OrchestratorChat.Core.Events;

/// <summary>
/// Base interface for all events in the system
/// </summary>
public interface IEvent
{
    /// <summary>
    /// Unique identifier for the event
    /// </summary>
    string Id { get; }
    
    /// <summary>
    /// Timestamp when the event occurred
    /// </summary>
    DateTime Timestamp { get; }
    
    /// <summary>
    /// Source that generated the event
    /// </summary>
    string Source { get; }
}