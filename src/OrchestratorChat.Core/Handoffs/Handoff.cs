namespace OrchestratorChat.Core.Handoffs;

/// <summary>
/// Implementation of a handoff request
/// </summary>
public class Handoff : IHandoff
{
    /// <summary>
    /// Unique identifier for the handoff
    /// </summary>
    public string Id { get; set; } = string.Empty;
    
    /// <summary>
    /// ID of the session this handoff belongs to
    /// </summary>
    public string SessionId { get; set; } = string.Empty;
    
    /// <summary>
    /// ID of the orchestration step being handed off
    /// </summary>
    public string StepId { get; set; } = string.Empty;
    
    /// <summary>
    /// ID of the agent initiating the handoff
    /// </summary>
    public string FromAgentId { get; set; } = string.Empty;
    
    /// <summary>
    /// ID of the agent receiving the handoff
    /// </summary>
    public string ToAgentId { get; set; } = string.Empty;
    
    /// <summary>
    /// Reason for the handoff
    /// </summary>
    public string Reason { get; set; } = string.Empty;
    
    /// <summary>
    /// Context information for the handoff
    /// </summary>
    public HandoffContext Context { get; set; } = new();
    
    /// <summary>
    /// Current status of the handoff
    /// </summary>
    public HandoffStatus Status { get; set; }
    
    /// <summary>
    /// When the handoff was initiated
    /// </summary>
    public DateTime InitiatedAt { get; set; }
    
    /// <summary>
    /// When the handoff was acknowledged (null if not yet acknowledged)
    /// </summary>
    public DateTime? AcknowledgedAt { get; set; }
}