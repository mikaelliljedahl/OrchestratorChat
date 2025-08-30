namespace OrchestratorChat.Core.Agents;

/// <summary>
/// Result of agent initialization process
/// </summary>
public class AgentInitializationResult
{
    /// <summary>
    /// Whether initialization was successful
    /// </summary>
    public bool Success { get; set; }
    
    /// <summary>
    /// Error message if initialization failed
    /// </summary>
    public string ErrorMessage { get; set; }
    
    /// <summary>
    /// Agent capabilities discovered during initialization
    /// </summary>
    public AgentCapabilities Capabilities { get; set; }
    
    /// <summary>
    /// Time taken to initialize the agent
    /// </summary>
    public TimeSpan InitializationTime { get; set; }
}