using OrchestratorChat.Core.Approvals;

namespace OrchestratorChat.Core.Orchestration;

/// <summary>
/// Interface for handling approval requests in the orchestration system
/// </summary>
public interface IApprovalHandler
{
    /// <summary>
    /// Request approval for an operation
    /// </summary>
    /// <param name="sessionId">ID of the session</param>
    /// <param name="stepId">ID of the orchestration step</param>
    /// <param name="requestedBy">ID of the agent requesting approval</param>
    /// <param name="requestType">Type of approval request</param>
    /// <param name="details">Details about what requires approval</param>
    /// <param name="timeout">Timeout for the approval request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The created approval request</returns>
    Task<Approval> RequestApprovalAsync(
        string sessionId,
        string stepId,
        string requestedBy,
        ApprovalType requestType,
        string details,
        TimeSpan timeout,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Approve a pending approval request
    /// </summary>
    /// <param name="approvalId">ID of the approval to approve</param>
    /// <param name="approvedBy">ID of the agent approving the request</param>
    /// <param name="reason">Reason for approval (optional)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The updated approval</returns>
    Task<Approval> ApproveAsync(
        string approvalId,
        string approvedBy,
        string? reason = null,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Reject a pending approval request
    /// </summary>
    /// <param name="approvalId">ID of the approval to reject</param>
    /// <param name="rejectedBy">ID of the agent rejecting the request</param>
    /// <param name="reason">Reason for rejection</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The updated approval</returns>
    Task<Approval> RejectAsync(
        string approvalId,
        string rejectedBy,
        string reason,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get an approval by its ID
    /// </summary>
    /// <param name="approvalId">ID of the approval</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The approval if found, null otherwise</returns>
    Task<Approval?> GetApprovalAsync(
        string approvalId,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get all pending approvals for a session
    /// </summary>
    /// <param name="sessionId">ID of the session</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of pending approvals for the session</returns>
    Task<IReadOnlyList<Approval>> GetPendingApprovalsAsync(
        string sessionId,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get all approvals for a session
    /// </summary>
    /// <param name="sessionId">ID of the session</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of all approvals for the session</returns>
    Task<IReadOnlyList<Approval>> GetSessionApprovalsAsync(
        string sessionId,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Wait for approval response or timeout
    /// </summary>
    /// <param name="approvalId">ID of the approval to wait for</param>
    /// <param name="timeout">Maximum time to wait</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if approved, false if rejected or timed out</returns>
    Task<bool> WaitForApprovalAsync(
        string approvalId,
        TimeSpan timeout,
        CancellationToken cancellationToken = default);
}