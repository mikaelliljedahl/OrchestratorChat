namespace OrchestratorChat.Core.Approvals;

/// <summary>
/// Interface for approval requests in the orchestration system
/// </summary>
public interface IApproval
{
    /// <summary>
    /// Unique identifier for the approval request
    /// </summary>
    string Id { get; }
    
    /// <summary>
    /// ID of the session this approval belongs to
    /// </summary>
    string SessionId { get; }
    
    /// <summary>
    /// ID of the orchestration step this approval is for
    /// </summary>
    string StepId { get; }
    
    /// <summary>
    /// ID of the agent requesting approval
    /// </summary>
    string RequestedBy { get; }
    
    /// <summary>
    /// Type of approval request
    /// </summary>
    ApprovalType RequestType { get; }
    
    /// <summary>
    /// Details about what requires approval
    /// </summary>
    string Details { get; }
    
    /// <summary>
    /// Current status of the approval
    /// </summary>
    ApprovalStatus Status { get; }
    
    /// <summary>
    /// ID of the agent who approved the request (null if not approved)
    /// </summary>
    string? ApprovedBy { get; }
    
    /// <summary>
    /// ID of the agent who rejected the request (null if not rejected)
    /// </summary>
    string? RejectedBy { get; }
    
    /// <summary>
    /// Reason for approval or rejection
    /// </summary>
    string? Reason { get; }
    
    /// <summary>
    /// When the approval was requested
    /// </summary>
    DateTime RequestedAt { get; }
    
    /// <summary>
    /// When the approval was responded to (null if still pending)
    /// </summary>
    DateTime? RespondedAt { get; }
}