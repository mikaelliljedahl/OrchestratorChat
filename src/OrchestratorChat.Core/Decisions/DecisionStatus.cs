namespace OrchestratorChat.Core.Decisions;

/// <summary>
/// Enumeration of decision statuses in the orchestration system
/// </summary>
public enum DecisionStatus
{
    /// <summary>
    /// Decision has been proposed but not yet in deliberation
    /// </summary>
    Proposed,
    
    /// <summary>
    /// Decision is in deliberation phase with team discussion
    /// </summary>
    Deliberating,
    
    /// <summary>
    /// Decision is in voting phase with team members casting votes
    /// </summary>
    Voting,
    
    /// <summary>
    /// Decision has been committed and finalized
    /// </summary>
    Committed,
    
    /// <summary>
    /// Decision has been rejected and will not proceed
    /// </summary>
    Rejected
}