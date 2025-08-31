namespace OrchestratorChat.Core.Events;

/// <summary>
/// Event raised when a decision is proposed in the orchestration system
/// </summary>
public class DecisionProposedEvent : IEvent
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
    /// ID of the decision that was proposed
    /// </summary>
    public string DecisionId { get; set; } = string.Empty;
    
    /// <summary>
    /// ID of the session the decision belongs to
    /// </summary>
    public string SessionId { get; set; } = string.Empty;
    
    /// <summary>
    /// ID of the agent that proposed the decision
    /// </summary>
    public string ProposedBy { get; set; } = string.Empty;
    
    /// <summary>
    /// The proposal text describing what needs to be decided
    /// </summary>
    public string Proposal { get; set; } = string.Empty;
    
    /// <summary>
    /// Available options for this decision
    /// </summary>
    public List<string> Options { get; set; } = new();
}