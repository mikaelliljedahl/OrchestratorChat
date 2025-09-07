namespace OrchestratorChat.Core.Timeline;

/// <summary>
/// Base interface for all timeline events in the orchestration system
/// </summary>
public interface ITimelineEvent
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
    /// Type of timeline event
    /// </summary>
    TimelineEventType EventType { get; }
    
    /// <summary>
    /// ID of the session this event belongs to
    /// </summary>
    string SessionId { get; }
    
    /// <summary>
    /// ID of the agent involved in this event (if applicable)
    /// </summary>
    string? AgentId { get; }
    
    /// <summary>
    /// Title or summary of the event for display purposes
    /// </summary>
    string Title { get; }
    
    /// <summary>
    /// Detailed description of the event
    /// </summary>
    string Description { get; }
    
    /// <summary>
    /// Additional metadata about the event
    /// </summary>
    Dictionary<string, object> Metadata { get; }
}