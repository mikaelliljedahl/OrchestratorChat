namespace OrchestratorChat.Core.Events;

/// <summary>
/// Event published when an approval is granted
/// </summary>
public class ApprovalGrantedEvent : IEvent
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
    /// ID of the agent who approved the request
    /// </summary>
    public string ApprovedBy { get; set; } = string.Empty;
    
    /// <summary>
    /// Reason for approval (optional)
    /// </summary>
    public string? Reason { get; set; }
    
    /// <summary>
    /// When the approval was granted
    /// </summary>
    public DateTime ApprovedAt { get; set; } = DateTime.UtcNow;
}