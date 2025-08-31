using OrchestratorChat.Core.Handoffs;

namespace OrchestratorChat.Core.Events;

/// <summary>
/// Event published when a handoff is acknowledged
/// </summary>
public class HandoffAcknowledgedEvent : IEvent
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
    /// The handoff that was acknowledged
    /// </summary>
    public Handoff Handoff { get; }
    
    /// <summary>
    /// Initializes a new instance of the HandoffAcknowledgedEvent
    /// </summary>
    /// <param name="handoff">The handoff that was acknowledged</param>
    public HandoffAcknowledgedEvent(Handoff handoff)
    {
        Id = Guid.NewGuid().ToString();
        Timestamp = DateTime.UtcNow;
        Source = "HandoffHandler";
        Handoff = handoff ?? throw new ArgumentNullException(nameof(handoff));
    }
}