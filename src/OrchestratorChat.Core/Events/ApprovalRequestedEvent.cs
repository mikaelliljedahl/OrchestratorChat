using OrchestratorChat.Core.Approvals;

namespace OrchestratorChat.Core.Events;

/// <summary>
/// Event published when an approval is requested
/// </summary>
public class ApprovalRequestedEvent : IEvent
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
    public string Source { get; set; } = "ApprovalHandler";
    
    /// <summary>
    /// Unique identifier for the approval request
    /// </summary>
    public string ApprovalId { get; set; } = string.Empty;
    
    /// <summary>
    /// ID of the session this approval belongs to
    /// </summary>
    public string SessionId { get; set; } = string.Empty;
    
    /// <summary>
    /// ID of the orchestration step this approval is for
    /// </summary>
    public string StepId { get; set; } = string.Empty;
    
    /// <summary>
    /// ID of the agent requesting approval
    /// </summary>
    public string RequestedBy { get; set; } = string.Empty;
    
    /// <summary>
    /// Type of approval request
    /// </summary>
    public ApprovalType RequestType { get; set; }
    
    /// <summary>
    /// Details about what requires approval
    /// </summary>
    public string Details { get; set; } = string.Empty;
    
    /// <summary>
    /// When the approval was requested
    /// </summary>
    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
}