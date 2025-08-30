namespace OrchestratorChat.Core.Events;

/// <summary>
/// Event raised when a session is ended
/// </summary>
public class SessionEndedEvent : IEvent
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
    /// ID of the ended session
    /// </summary>
    public string SessionId { get; }
    
    /// <summary>
    /// Reason for ending the session
    /// </summary>
    public string Reason { get; }
    
    /// <summary>
    /// Initializes a new instance of the SessionEndedEvent
    /// </summary>
    /// <param name="sessionId">ID of the ended session</param>
    /// <param name="reason">Reason for ending the session</param>
    public SessionEndedEvent(string sessionId, string reason = "")
    {
        Id = Guid.NewGuid().ToString();
        SessionId = sessionId ?? throw new ArgumentNullException(nameof(sessionId));
        Reason = reason;
        Timestamp = DateTime.UtcNow;
        Source = "SessionManager";
    }
}