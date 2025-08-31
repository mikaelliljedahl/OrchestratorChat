namespace OrchestratorChat.Core.Handoffs;

/// <summary>
/// Status of a handoff request
/// </summary>
public enum HandoffStatus
{
    /// <summary>
    /// Handoff has been initiated and is waiting for acknowledgment
    /// </summary>
    Initiated,
    
    /// <summary>
    /// Handoff has been acknowledged by the target agent
    /// </summary>
    Acknowledged,
    
    /// <summary>
    /// Handoff has been rejected by the target agent
    /// </summary>
    Rejected,
    
    /// <summary>
    /// Handoff has timed out without response
    /// </summary>
    Timeout
}