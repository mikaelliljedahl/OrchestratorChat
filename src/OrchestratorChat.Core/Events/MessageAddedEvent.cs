using OrchestratorChat.Core.Messages;

namespace OrchestratorChat.Core.Events;

/// <summary>
/// Event raised when a message is added to a session
/// </summary>
public class MessageAddedEvent : IEvent
{
    /// <summary>
    /// Unique identifier for the event
    /// </summary>
    public string Id { get; }
    
    /// <summary>
    /// Timestamp when the event occurred
    /// </summary>
    public DateTime Timestamp { get; }
    
    /// <summary>
    /// Source that generated the event
    /// </summary>
    public string Source { get; }
    
    /// <summary>
    /// ID of the session the message was added to
    /// </summary>
    public string SessionId { get; }
    
    /// <summary>
    /// The message that was added
    /// </summary>
    public AgentMessage Message { get; }
    
    /// <summary>
    /// Initializes a new instance of the MessageAddedEvent
    /// </summary>
    /// <param name="sessionId">ID of the session</param>
    /// <param name="message">The added message</param>
    public MessageAddedEvent(string sessionId, AgentMessage message)
    {
        Id = Guid.NewGuid().ToString();
        SessionId = sessionId ?? throw new ArgumentNullException(nameof(sessionId));
        Message = message ?? throw new ArgumentNullException(nameof(message));
        Timestamp = DateTime.UtcNow;
        Source = "SessionManager";
    }
}