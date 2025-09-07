using OrchestratorChat.Core.Decisions;
using OrchestratorChat.Core.Events;

namespace OrchestratorChat.Core.Timeline;

/// <summary>
/// Timeline event representing a decision in the orchestration process
/// </summary>
public class DecisionTimelineEvent : ITimelineEvent
{
    /// <summary>
    /// Unique identifier for the event
    /// </summary>
    public string Id { get; }
    
    /// <summary>
    /// Timestamp when the event occurred
    /// </summary>
    public DateTime Timestamp { get; }
    
    /// <summary>
    /// Type of timeline event
    /// </summary>
    public TimelineEventType EventType { get; } = TimelineEventType.Decision;
    
    /// <summary>
    /// ID of the session this event belongs to
    /// </summary>
    public string SessionId { get; }
    
    /// <summary>
    /// ID of the agent involved in this event
    /// </summary>
    public string? AgentId { get; }
    
    /// <summary>
    /// Title or summary of the event for display purposes
    /// </summary>
    public string Title { get; }
    
    /// <summary>
    /// Detailed description of the event
    /// </summary>
    public string Description { get; }
    
    /// <summary>
    /// Additional metadata about the event
    /// </summary>
    public Dictionary<string, object> Metadata { get; }
    
    /// <summary>
    /// The decision ID this event relates to
    /// </summary>
    public string DecisionId { get; }
    
    /// <summary>
    /// The phase of the decision lifecycle this event represents
    /// </summary>
    public DecisionPhase Phase { get; }
    
    /// <summary>
    /// Status of the decision
    /// </summary>
    public DecisionStatus Status { get; }
    
    /// <summary>
    /// Initializes a new DecisionTimelineEvent from a DecisionProposedEvent
    /// </summary>
    /// <param name="proposedEvent">The decision proposed event</param>
    public DecisionTimelineEvent(DecisionProposedEvent proposedEvent)
    {
        Id = proposedEvent.Id;
        Timestamp = proposedEvent.Timestamp;
        SessionId = proposedEvent.SessionId;
        AgentId = proposedEvent.ProposedBy;
        DecisionId = proposedEvent.DecisionId;
        Phase = DecisionPhase.Proposed;
        Status = DecisionStatus.Proposed;
        
        Title = "Decision Proposed";
        Description = proposedEvent.Proposal;
        
        Metadata = new Dictionary<string, object>
        {
            ["DecisionId"] = proposedEvent.DecisionId,
            ["ProposedBy"] = proposedEvent.ProposedBy,
            ["Options"] = proposedEvent.Options,
            ["Phase"] = Phase.ToString(),
            ["Status"] = Status.ToString()
        };
    }
    
    /// <summary>
    /// Initializes a new DecisionTimelineEvent from a DecisionCommittedEvent
    /// </summary>
    /// <param name="committedEvent">The decision committed event</param>
    public DecisionTimelineEvent(DecisionCommittedEvent committedEvent)
    {
        Id = committedEvent.Id;
        Timestamp = committedEvent.Timestamp;
        SessionId = committedEvent.SessionId;
        AgentId = null; // No specific agent field in DecisionCommittedEvent
        DecisionId = committedEvent.DecisionId;
        Phase = DecisionPhase.Committed;
        Status = DecisionStatus.Committed;
        
        Title = "Decision Committed";
        Description = committedEvent.Rationale;
        
        Metadata = new Dictionary<string, object>
        {
            ["DecisionId"] = committedEvent.DecisionId,
            ["Rationale"] = committedEvent.Rationale,
            ["Phase"] = Phase.ToString(),
            ["Status"] = Status.ToString()
        };
        
        if (!string.IsNullOrEmpty(committedEvent.Consequences))
        {
            Metadata["Consequences"] = committedEvent.Consequences;
        }
        
        if (committedEvent.Votes?.Any() == true)
        {
            Metadata["Votes"] = committedEvent.Votes;
        }
    }
}

/// <summary>
/// Represents the phase of a decision in its lifecycle
/// </summary>
public enum DecisionPhase
{
    /// <summary>
    /// Decision has been proposed but not yet voted on
    /// </summary>
    Proposed,
    
    /// <summary>
    /// Decision is currently being voted on
    /// </summary>
    Voting,
    
    /// <summary>
    /// Decision has been committed/finalized
    /// </summary>
    Committed
}