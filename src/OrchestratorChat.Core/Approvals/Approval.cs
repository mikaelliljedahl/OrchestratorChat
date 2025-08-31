namespace OrchestratorChat.Core.Approvals;

/// <summary>
/// Implementation of an approval request
/// </summary>
public class Approval : IApproval
{
    /// <summary>
    /// Unique identifier for the approval request
    /// </summary>
    public string Id { get; set; } = string.Empty;
    
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
    /// Current status of the approval
    /// </summary>
    public ApprovalStatus Status { get; set; } = ApprovalStatus.Pending;
    
    /// <summary>
    /// ID of the agent who approved the request (null if not approved)
    /// </summary>
    public string? ApprovedBy { get; set; }
    
    /// <summary>
    /// ID of the agent who rejected the request (null if not rejected)
    /// </summary>
    public string? RejectedBy { get; set; }
    
    /// <summary>
    /// Reason for approval or rejection
    /// </summary>
    public string? Reason { get; set; }
    
    /// <summary>
    /// When the approval was requested
    /// </summary>
    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// When the approval was responded to (null if still pending)
    /// </summary>
    public DateTime? RespondedAt { get; set; }
}