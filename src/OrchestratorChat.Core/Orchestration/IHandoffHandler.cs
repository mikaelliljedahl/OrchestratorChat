using OrchestratorChat.Core.Handoffs;

namespace OrchestratorChat.Core.Orchestration;

/// <summary>
/// Interface for handling handoff requests in the orchestration system
/// </summary>
public interface IHandoffHandler
{
    /// <summary>
    /// Initiates a handoff request from one agent to another
    /// </summary>
    /// <param name="sessionId">ID of the session</param>
    /// <param name="stepId">ID of the orchestration step</param>
    /// <param name="fromAgentId">ID of the agent initiating the handoff</param>
    /// <param name="toAgentId">ID of the agent to receive the handoff</param>
    /// <param name="reason">Reason for the handoff</param>
    /// <param name="context">Context information for the handoff</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The created handoff request</returns>
    Task<Handoff> InitiateHandoffAsync(
        string sessionId,
        string stepId,
        string fromAgentId,
        string toAgentId,
        string reason,
        HandoffContext context,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Acknowledges a handoff request, accepting ownership of the step
    /// </summary>
    /// <param name="handoffId">ID of the handoff to acknowledge</param>
    /// <param name="acknowledgingAgentId">ID of the agent acknowledging the handoff</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The updated handoff</returns>
    Task<Handoff> AcknowledgeHandoffAsync(
        string handoffId,
        string acknowledgingAgentId,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Rejects a handoff request, declining ownership of the step
    /// </summary>
    /// <param name="handoffId">ID of the handoff to reject</param>
    /// <param name="rejectingAgentId">ID of the agent rejecting the handoff</param>
    /// <param name="reason">Reason for rejection</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The updated handoff</returns>
    Task<Handoff> RejectHandoffAsync(
        string handoffId,
        string rejectingAgentId,
        string reason,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets a handoff by its ID
    /// </summary>
    /// <param name="handoffId">ID of the handoff</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The handoff if found, null otherwise</returns>
    Task<Handoff?> GetHandoffAsync(
        string handoffId,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets all handoffs for a specific step
    /// </summary>
    /// <param name="sessionId">ID of the session</param>
    /// <param name="stepId">ID of the step</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of handoffs for the step</returns>
    Task<IReadOnlyList<Handoff>> GetStepHandoffsAsync(
        string sessionId,
        string stepId,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets all pending handoffs for an agent
    /// </summary>
    /// <param name="agentId">ID of the agent</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of pending handoffs for the agent</returns>
    Task<IReadOnlyList<Handoff>> GetPendingHandoffsAsync(
        string agentId,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Waits for handoff acknowledgment or timeout
    /// </summary>
    /// <param name="handoffId">ID of the handoff to wait for</param>
    /// <param name="timeout">Maximum time to wait</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if acknowledged, false if rejected or timed out</returns>
    Task<bool> WaitForHandoffResponseAsync(
        string handoffId,
        TimeSpan timeout,
        CancellationToken cancellationToken = default);
}