namespace OrchestratorChat.Core.Agents;

/// <summary>
/// Event arguments for agent status change events
/// </summary>
public class AgentStatusChangedEventArgs : EventArgs
{
    /// <summary>
    /// ID of the agent whose status changed
    /// </summary>
    public string AgentId { get; set; }
    
    /// <summary>
    /// Previous status of the agent
    /// </summary>
    public AgentStatus OldStatus { get; set; }
    
    /// <summary>
    /// New status of the agent
    /// </summary>
    public AgentStatus NewStatus { get; set; }
    
    /// <summary>
    /// Reason for the status change
    /// </summary>
    public string Reason { get; set; }
    
    /// <summary>
    /// Timestamp when the status change occurred
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}