namespace OrchestratorChat.Core.Events;

/// <summary>
/// Event raised when a vote is received for a decision
/// </summary>
public class DecisionVoteReceivedEvent : IEvent
{
    /// <summary>
    /// Unique identifier for the event
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();
    
    /// <summary>
    /// Timestamp when the event occurred
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Source that generated the event
    /// </summary>
    public string Source { get; set; } = "DecisionHandler";
    
    /// <summary>
    /// ID of the decision that received the vote
    /// </summary>
    public string DecisionId { get; set; } = string.Empty;
    
    /// <summary>
    /// ID of the agent that cast the vote
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
}