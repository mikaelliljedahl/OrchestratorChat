using OrchestratorChat.Core.Decisions;

namespace OrchestratorChat.Core.Events;

/// <summary>
/// Event raised when a decision is committed in the orchestration system
/// </summary>
public class DecisionCommittedEvent : IEvent
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
    /// ID of the decision that was committed
    /// </summary>
    public string DecisionId { get; set; } = string.Empty;
    
    /// <summary>
    /// ID of the session the decision belongs to
    /// </summary>
    public string SessionId { get; set; } = string.Empty;
    
    /// <summary>
    /// Rationale for the final decision
    /// </summary>
    public string Rationale { get; set; } = string.Empty;
    
    /// <summary>
    /// Consequences or impact of the decision
    /// </summary>
    public string? Consequences { get; set; }
    
    /// <summary>
    /// All votes that were cast on this decision
    /// </summary>
    public List<DecisionVote> Votes { get; set; } = new();
}