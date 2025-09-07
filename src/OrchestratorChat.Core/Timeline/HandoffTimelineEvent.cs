using OrchestratorChat.Core.Events;
using OrchestratorChat.Core.Handoffs;

namespace OrchestratorChat.Core.Timeline;

/// <summary>
/// Timeline event representing a handoff between agents
/// </summary>
public class HandoffTimelineEvent : ITimelineEvent
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
    public TimelineEventType EventType { get; } = TimelineEventType.Handoff;
    
    /// <summary>
    /// ID of the session this event belongs to
    /// </summary>
    public string SessionId { get; }
    
    /// <summary>
    /// ID of the agent involved in this event (the initiating agent)
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
    /// The handoff ID this event relates to
    /// </summary>
    public string HandoffId { get; }
    
    /// <summary>
    /// The phase of the handoff lifecycle this event represents
    /// </summary>
    public HandoffPhase Phase { get; }
    
    /// <summary>
    /// ID of the agent receiving the handoff
    /// </summary>
    public string ToAgentId { get; }
    
    /// <summary>
    /// Initializes a new HandoffTimelineEvent from a HandoffInitiatedEvent
    /// </summary>
    /// <param name="initiatedEvent">The handoff initiated event</param>
    public HandoffTimelineEvent(HandoffInitiatedEvent initiatedEvent)
    {
        Id = initiatedEvent.Id;
        Timestamp = initiatedEvent.Timestamp;
        SessionId = initiatedEvent.Handoff.SessionId;
        AgentId = initiatedEvent.Handoff.FromAgentId;
        HandoffId = initiatedEvent.Handoff.Id;
        ToAgentId = initiatedEvent.Handoff.ToAgentId;
        Phase = HandoffPhase.Initiated;
        
        Title = "Handoff Initiated";
        Description = $"Handoff from {initiatedEvent.Handoff.FromAgentId} to {initiatedEvent.Handoff.ToAgentId}";
        
        Metadata = new Dictionary<string, object>
        {
            ["HandoffId"] = HandoffId,
            ["FromAgent"] = initiatedEvent.Handoff.FromAgentId,
            ["ToAgent"] = ToAgentId,
            ["Phase"] = Phase.ToString(),
            ["Status"] = initiatedEvent.Handoff.Status.ToString(),
            ["Context"] = initiatedEvent.Handoff.Context.ToString()
        };
        
        if (!string.IsNullOrEmpty(initiatedEvent.Handoff.Reason))
        {
            Metadata["Reason"] = initiatedEvent.Handoff.Reason;
        }
    }
    
    /// <summary>
    /// Initializes a new HandoffTimelineEvent from a HandoffAcknowledgedEvent
    /// </summary>
    /// <param name="acknowledgedEvent">The handoff acknowledged event</param>
    public HandoffTimelineEvent(HandoffAcknowledgedEvent acknowledgedEvent)
    {
        Id = acknowledgedEvent.Id;
        Timestamp = acknowledgedEvent.Timestamp;
        SessionId = acknowledgedEvent.Handoff.SessionId;
        AgentId = acknowledgedEvent.Handoff.ToAgentId;
        HandoffId = acknowledgedEvent.Handoff.Id;
        ToAgentId = acknowledgedEvent.Handoff.ToAgentId;
        Phase = HandoffPhase.Acknowledged;
        
        Title = "Handoff Acknowledged";
        Description = $"Handoff acknowledged by {acknowledgedEvent.Handoff.ToAgentId}";
        
        Metadata = new Dictionary<string, object>
        {
            ["HandoffId"] = HandoffId,
            ["FromAgent"] = acknowledgedEvent.Handoff.FromAgentId,
            ["ToAgent"] = ToAgentId,
            ["Phase"] = Phase.ToString(),
            ["Status"] = acknowledgedEvent.Handoff.Status.ToString(),
            ["AcknowledgedAt"] = acknowledgedEvent.Handoff.AcknowledgedAt?.ToString("O") ?? string.Empty
        };
    }
    
    /// <summary>
    /// Initializes a new HandoffTimelineEvent from a HandoffRejectedEvent
    /// </summary>
    /// <param name="rejectedEvent">The handoff rejected event</param>
    public HandoffTimelineEvent(HandoffRejectedEvent rejectedEvent)
    {
        Id = rejectedEvent.Id;
        Timestamp = rejectedEvent.Timestamp;
        SessionId = rejectedEvent.Handoff.SessionId;
        AgentId = rejectedEvent.Handoff.ToAgentId;
        HandoffId = rejectedEvent.Handoff.Id;
        ToAgentId = rejectedEvent.Handoff.ToAgentId;
        Phase = HandoffPhase.Rejected;
        
        Title = "Handoff Rejected";
        Description = $"Handoff rejected by {rejectedEvent.Handoff.ToAgentId}";
        
        Metadata = new Dictionary<string, object>
        {
            ["HandoffId"] = HandoffId,
            ["FromAgent"] = rejectedEvent.Handoff.FromAgentId,
            ["ToAgent"] = ToAgentId,
            ["Phase"] = Phase.ToString(),
            ["Status"] = rejectedEvent.Handoff.Status.ToString()
        };
        
        if (!string.IsNullOrEmpty(rejectedEvent.Reason))
        {
            Metadata["RejectionReason"] = rejectedEvent.Reason;
        }
    }
}

/// <summary>
/// Represents the phase of a handoff in its lifecycle
/// </summary>
public enum HandoffPhase
{
    /// <summary>
    /// Handoff has been initiated but not yet acknowledged
    /// </summary>
    Initiated,
    
    /// <summary>
    /// Handoff has been acknowledged by the receiving agent
    /// </summary>
    Acknowledged,
    
    /// <summary>
    /// Handoff has been rejected by the receiving agent
    /// </summary>
    Rejected
}