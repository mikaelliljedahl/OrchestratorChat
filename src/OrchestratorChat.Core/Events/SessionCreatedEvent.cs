using OrchestratorChat.Core.Sessions;

namespace OrchestratorChat.Core.Events;

/// <summary>
/// Event raised when a new session is created
/// </summary>
public class SessionCreatedEvent : IEvent
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
    /// The newly created session
    /// </summary>
    public Session Session { get; }
    
    /// <summary>
    /// Initializes a new instance of the SessionCreatedEvent
    /// </summary>
    /// <param name="session">The created session</param>
    public SessionCreatedEvent(Session session)
    {
        Id = Guid.NewGuid().ToString();
        Session = session ?? throw new ArgumentNullException(nameof(session));
        Timestamp = DateTime.UtcNow;
        Source = "SessionManager";
    }
}