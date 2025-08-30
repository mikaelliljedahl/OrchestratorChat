namespace OrchestratorChat.Core.Agents;

/// <summary>
/// Represents detailed status information about an agent
/// </summary>
public class AgentStatusInfo
{
    /// <summary>
    /// Current status of the agent
    /// </summary>
    public AgentStatus Status { get; set; }
    
    /// <summary>
    /// Unique identifier of the agent
    /// </summary>
    public string AgentId { get; set; } = string.Empty;
    
    /// <summary>
    /// Name of the agent
    /// </summary>
    public string AgentName { get; set; } = string.Empty;
    
    /// <summary>
    /// Type of the agent
    /// </summary>
    public AgentType Type { get; set; }
    
    /// <summary>
    /// Whether the agent is healthy and functioning properly
    /// </summary>
    public bool IsHealthy { get; set; }
    
    /// <summary>
    /// Last time the agent had any activity
    /// </summary>
    public DateTime? LastActivity { get; set; }
    
    /// <summary>
    /// Agent capabilities and metadata
    /// </summary>
    public AgentCapabilities? Capabilities { get; set; }
    
    /// <summary>
    /// Working directory of the agent
    /// </summary>
    public string? WorkingDirectory { get; set; }
    
    /// <summary>
    /// Additional metadata about the agent
    /// </summary>
    public Dictionary<string, object>? Metadata { get; set; }
}