namespace OrchestratorChat.Core.Decisions;

/// <summary>
/// Represents a vote cast by an agent on a decision
/// </summary>
public class DecisionVote
{
    /// <summary>
    /// ID of the agent that cast this vote
    /// </summary>
    public string AgentId { get; set; } = string.Empty;
    
    /// <summary>
    /// The option the agent voted for
    /// </summary>
    public string Option { get; set; } = string.Empty;
    
    /// <summary>
    /// Reason provided by the agent for their vote
    /// </summary>
    public string? Reason { get; set; }
    
    /// <summary>
    /// When the vote was cast
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}