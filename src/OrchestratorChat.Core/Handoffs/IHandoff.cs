namespace OrchestratorChat.Core.Handoffs;

/// <summary>
/// Interface for handoff requests in the orchestration system
/// </summary>
public interface IHandoff
{
    /// <summary>
    /// Unique identifier for the handoff
    /// </summary>
    string Id { get; }
    
    /// <summary>
    /// ID of the session this handoff belongs to
    /// </summary>
    string SessionId { get; }
    
    /// <summary>
    /// ID of the orchestration step being handed off
    /// </summary>
    string StepId { get; }
    
    /// <summary>
    /// ID of the agent initiating the handoff
    /// </summary>
    string FromAgentId { get; }
    
    /// <summary>
    /// ID of the agent receiving the handoff
    /// </summary>
    string ToAgentId { get; }
    
    /// <summary>
    /// Reason for the handoff
    /// </summary>
    string Reason { get; }
    
    /// <summary>
    /// Context information for the handoff
    /// </summary>
    HandoffContext Context { get; }
    
    /// <summary>
    /// Current status of the handoff
    /// </summary>
    HandoffStatus Status { get; }
    
    /// <summary>
    /// When the handoff was initiated
    /// </summary>
    DateTime InitiatedAt { get; }
    
    /// <summary>
    /// When the handoff was acknowledged (null if not yet acknowledged)
    /// </summary>
    DateTime? AcknowledgedAt { get; }
}