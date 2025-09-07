using OrchestratorChat.Core.Approvals;
using OrchestratorChat.Core.Events;

namespace OrchestratorChat.Core.Timeline;

/// <summary>
/// Timeline event representing an approval in the orchestration process
/// </summary>
public class ApprovalTimelineEvent : ITimelineEvent
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
    /// Type of timeline event
    /// </summary>
    public TimelineEventType EventType { get; } = TimelineEventType.Approval;
    
    /// <summary>
    /// ID of the session this event belongs to
    /// </summary>
    public string SessionId { get; }
    
    /// <summary>
    /// ID of the agent involved in this event
    /// </summary>
    public string? AgentId { get; }
    
    /// <summary>
    /// Title or summary of the event for display purposes
    /// </summary>
    public string Title { get; }
    
    /// <summary>
    /// Detailed description of the event
    /// </summary>
    public string Description { get; }
    
    /// <summary>
    /// Additional metadata about the event
    /// </summary>
    public Dictionary<string, object> Metadata { get; }
    
    /// <summary>
    /// The approval ID this event relates to
    /// </summary>
    public string ApprovalId { get; }
    
    /// <summary>
    /// The phase of the approval lifecycle this event represents
    /// </summary>
    public ApprovalPhase Phase { get; }
    
    /// <summary>
    /// Type of approval
    /// </summary>
    public ApprovalType ApprovalType { get; }
    
    /// <summary>
    /// Initializes a new ApprovalTimelineEvent from an ApprovalRequestedEvent
    /// </summary>
    /// <param name="requestedEvent">The approval requested event</param>
    public ApprovalTimelineEvent(ApprovalRequestedEvent requestedEvent)
    {
        Id = requestedEvent.Id;
        Timestamp = requestedEvent.Timestamp;
        SessionId = requestedEvent.SessionId;
        AgentId = requestedEvent.RequestedBy;
        ApprovalId = requestedEvent.ApprovalId;
        Phase = ApprovalPhase.Requested;
        ApprovalType = requestedEvent.RequestType;
        
        Title = "Approval Requested";
        Description = requestedEvent.Details;
        
        Metadata = new Dictionary<string, object>
        {
            ["ApprovalId"] = ApprovalId,
            ["RequestedBy"] = requestedEvent.RequestedBy,
            ["ApprovalType"] = ApprovalType.ToString(),
            ["Phase"] = Phase.ToString(),
            ["StepId"] = requestedEvent.StepId,
            ["RequestedAt"] = requestedEvent.RequestedAt.ToString("O")
        };
    }
    
    /// <summary>
    /// Initializes a new ApprovalTimelineEvent from an ApprovalGrantedEvent
    /// </summary>
    /// <param name="grantedEvent">The approval granted event</param>
    public ApprovalTimelineEvent(ApprovalGrantedEvent grantedEvent)
    {
        Id = grantedEvent.Id;
        Timestamp = grantedEvent.Timestamp;
        SessionId = grantedEvent.SessionId;
        AgentId = grantedEvent.ApprovedBy;
        ApprovalId = grantedEvent.ApprovalId;
        Phase = ApprovalPhase.Granted;
        ApprovalType = Core.Approvals.ApprovalType.ToolExecution; // Default as no type field in event
        
        Title = "Approval Granted";
        Description = $"Approval granted by {grantedEvent.ApprovedBy}";
        
        Metadata = new Dictionary<string, object>
        {
            ["ApprovalId"] = ApprovalId,
            ["ApprovedBy"] = grantedEvent.ApprovedBy,
            ["Phase"] = Phase.ToString(),
            ["ApprovedAt"] = grantedEvent.ApprovedAt.ToString("O")
        };
        
        if (!string.IsNullOrEmpty(grantedEvent.Reason))
        {
            Metadata["Reason"] = grantedEvent.Reason;
        }
    }
    
    /// <summary>
    /// Initializes a new ApprovalTimelineEvent from an ApprovalRejectedEvent
    /// </summary>
    /// <param name="rejectedEvent">The approval rejected event</param>
    public ApprovalTimelineEvent(ApprovalRejectedEvent rejectedEvent)
    {
        Id = rejectedEvent.Id;
        Timestamp = rejectedEvent.Timestamp;
        SessionId = rejectedEvent.SessionId;
        AgentId = rejectedEvent.RejectedBy;
        ApprovalId = rejectedEvent.ApprovalId;
        Phase = ApprovalPhase.Rejected;
        ApprovalType = Core.Approvals.ApprovalType.ToolExecution; // Default as no type field in event
        
        Title = "Approval Rejected";
        Description = rejectedEvent.Reason ?? "Approval was rejected";
        
        Metadata = new Dictionary<string, object>
        {
            ["ApprovalId"] = ApprovalId,
            ["RejectedBy"] = rejectedEvent.RejectedBy,
            ["Phase"] = Phase.ToString(),
            ["RejectedAt"] = rejectedEvent.RejectedAt.ToString("O"),
            ["Reason"] = rejectedEvent.Reason ?? string.Empty
        };
    }
}

/// <summary>
/// Represents the phase of an approval in its lifecycle
/// </summary>
public enum ApprovalPhase
{
    /// <summary>
    /// Approval has been requested but not yet responded to
    /// </summary>
    Requested,
    
    /// <summary>
    /// Approval has been granted
    /// </summary>
    Granted,
    
    /// <summary>
    /// Approval has been rejected
    /// </summary>
    Rejected
}