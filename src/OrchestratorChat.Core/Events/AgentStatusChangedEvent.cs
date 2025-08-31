using OrchestratorChat.Core.Agents;

namespace OrchestratorChat.Core.Events;

/// <summary>
/// Event raised when an agent's status changes
/// </summary>
public class AgentStatusChangedEvent : IEvent
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
    public string Source { get; set; } = string.Empty;
    
    /// <summary>
    /// ID of the agent whose status changed
    /// </summary>
    public string AgentId { get; set; } = string.Empty;
    
    /// <summary>
    /// Previous status of the agent
    /// </summary>
    public AgentStatus OldStatus { get; set; }
    
    /// <summary>
    /// New status of the agent
    /// </summary>
    public AgentStatus NewStatus { get; set; }
    
    /// <summary>
    /// Reason for the status change
    /// </summary>
    public string Reason { get; set; } = string.Empty;
}