using OrchestratorChat.Core.Messages;

namespace OrchestratorChat.Core.Events;

/// <summary>
/// Event raised when a message is received in the system
/// </summary>
public class MessageReceivedEvent : IEvent
{
    /// <summary>
    /// Unique identifier for the event
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();
    
    /// <summary>
    /// Timestamp when the event occurred
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Source that generated the event
    /// </summary>
    public string Source { get; set; }
    
    /// <summary>
    /// The message that was received
    /// </summary>
    public AgentMessage Message { get; set; }
    
    /// <summary>
    /// ID of the session the message belongs to
    /// </summary>
    public string SessionId { get; set; }
}