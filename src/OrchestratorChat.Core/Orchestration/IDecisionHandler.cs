using OrchestratorChat.Core.Decisions;

namespace OrchestratorChat.Core.Orchestration;

/// <summary>
/// Interface for handling decisions in the orchestration system
/// </summary>
public interface IDecisionHandler
{
    /// <summary>
    /// Propose a decision for team consideration
    /// </summary>
    /// <param name="sessionId">ID of the session</param>
    /// <param name="proposedBy">ID of the agent proposing the decision</param>
    /// <param name="proposal">The proposal text</param>
    /// <param name="options">Available options for the decision</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The created decision</returns>
    Task<Decision> ProposeAsync(
        string sessionId,
        string proposedBy, 
        string proposal,
        IEnumerable<string> options,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Start the deliberation phase for a decision
    /// </summary>
    /// <param name="decisionId">ID of the decision to deliberate</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The updated decision</returns>
    Task<Decision> DeliberateAsync(
        string decisionId,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Cast a vote on a decision
    /// </summary>
    /// <param name="decisionId">ID of the decision to vote on</param>
    /// <param name="agentId">ID of the agent casting the vote</param>
    /// <param name="option">The option being voted for</param>
    /// <param name="reason">Reason for the vote (optional)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The updated decision</returns>
    Task<Decision> VoteAsync(
        string decisionId,
        string agentId,
        string option,
        string? reason = null,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Commit a decision based on the votes
    /// </summary>
    /// <param name="decisionId">ID of the decision to commit</param>
    /// <param name="rationale">Rationale for the final decision</param>
    /// <param name="consequences">Consequences or impact of the decision</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The committed decision</returns>
    Task<Decision> CommitAsync(
        string decisionId,
        string rationale,
        string? consequences = null,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get a decision by its ID
    /// </summary>
    /// <param name="decisionId">ID of the decision</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The decision if found, null otherwise</returns>
    Task<Decision?> GetDecisionAsync(
        string decisionId,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get all decisions for a session
    /// </summary>
    /// <param name="sessionId">ID of the session</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of decisions for the session</returns>
    Task<IReadOnlyList<Decision>> GetSessionDecisionsAsync(
        string sessionId,
        CancellationToken cancellationToken = default);
}