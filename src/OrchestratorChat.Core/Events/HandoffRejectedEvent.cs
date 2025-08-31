using OrchestratorChat.Core.Handoffs;

namespace OrchestratorChat.Core.Events;

/// <summary>
/// Event published when a handoff is rejected
/// </summary>
public class HandoffRejectedEvent : IEvent
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
    /// The handoff that was rejected
    /// </summary>
    public Handoff Handoff { get; }
    
    /// <summary>
    /// Reason for the rejection
    /// </summary>
    public string Reason { get; }
    
    /// <summary>
    /// Initializes a new instance of the HandoffRejectedEvent
    /// </summary>
    /// <param name="handoff">The handoff that was rejected</param>
    /// <param name="reason">Reason for the rejection</param>
    public HandoffRejectedEvent(Handoff handoff, string reason)
    {
        Id = Guid.NewGuid().ToString();
        Timestamp = DateTime.UtcNow;
        Source = "HandoffHandler";
        Handoff = handoff ?? throw new ArgumentNullException(nameof(handoff));
        Reason = reason ?? throw new ArgumentNullException(nameof(reason));
    }
}