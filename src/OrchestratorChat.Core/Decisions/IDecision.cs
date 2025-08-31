namespace OrchestratorChat.Core.Decisions;

/// <summary>
/// Interface for decisions in the orchestration system
/// </summary>
public interface IDecision
{
    /// <summary>
    /// Unique identifier for the decision
    /// </summary>
    string Id { get; }
    
    /// <summary>
    /// ID of the session this decision belongs to
    /// </summary>
    string SessionId { get; }
    
    /// <summary>
    /// ID of the agent that proposed this decision
    /// </summary>
    string ProposedBy { get; }
    
    /// <summary>
    /// The proposal text describing what needs to be decided
    /// </summary>
    string Proposal { get; }
    
    /// <summary>
    /// Available options for this decision
    /// </summary>
    IReadOnlyList<string> Options { get; }
    
    /// <summary>
    /// Votes collected from team members
    /// </summary>
    IReadOnlyList<DecisionVote> Votes { get; }
    
    /// <summary>
    /// Current status of the decision
    /// </summary>
    DecisionStatus Status { get; }
    
    /// <summary>
    /// Rationale for the final decision (populated when committed)
    /// </summary>
    string? Rationale { get; }
    
    /// <summary>
    /// Consequences or impact of the decision (populated when committed)
    /// </summary>
    string? Consequences { get; }
    
    /// <summary>
    /// When the decision was created
    /// </summary>
    DateTime CreatedAt { get; }
    
    /// <summary>
    /// When the decision was committed (null if not yet committed)
    /// </summary>
    DateTime? CommittedAt { get; }
}