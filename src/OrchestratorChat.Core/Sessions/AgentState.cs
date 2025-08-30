using OrchestratorChat.Core.Agents;

namespace OrchestratorChat.Core.Sessions;

/// <summary>
/// Represents the state of an agent at a point in time
/// </summary>
public class AgentState
{
    /// <summary>
    /// ID of the agent
    /// </summary>
    public string AgentId { get; set; }
    
    /// <summary>
    /// Status of the agent at this state
    /// </summary>
    public AgentStatus Status { get; set; }
    
    /// <summary>
    /// Configuration of the agent at this state
    /// </summary>
    public AgentConfiguration Configuration { get; set; }
    
    /// <summary>
    /// Working directory of the agent at this state
    /// </summary>
    public string WorkingDirectory { get; set; }
    
    /// <summary>
    /// Additional state data for the agent
    /// </summary>
    public Dictionary<string, object> StateData { get; set; } = new();
    
    /// <summary>
    /// Timestamp when this state was captured
    /// </summary>
    public DateTime CapturedAt { get; set; } = DateTime.UtcNow;
}