namespace OrchestratorChat.Core.Approvals;

/// <summary>
/// Status of an approval request
/// </summary>
public enum ApprovalStatus
{
    /// <summary>
    /// Approval is waiting for a response
    /// </summary>
    Pending,
    
    /// <summary>
    /// Approval has been granted
    /// </summary>
    Approved,
    
    /// <summary>
    /// Approval has been rejected
    /// </summary>
    Rejected,
    
    /// <summary>
    /// Approval request timed out without a response
    /// </summary>
    Timeout
}