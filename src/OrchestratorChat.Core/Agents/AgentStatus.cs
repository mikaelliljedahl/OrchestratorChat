namespace OrchestratorChat.Core.Agents;

/// <summary>
/// Defines the possible states of an agent
/// </summary>
public enum AgentStatus
{
    /// <summary>
    /// Agent has not been initialized
    /// </summary>
    Uninitialized,
    
    /// <summary>
    /// Agent is in the process of initializing
    /// </summary>
    Initializing,
    
    /// <summary>
    /// Agent is ready to receive requests
    /// </summary>
    Ready,
    
    /// <summary>
    /// Agent is currently processing a request
    /// </summary>
    Busy,
    
    /// <summary>
    /// Agent has encountered an error
    /// </summary>
    Error,
    
    /// <summary>
    /// Agent has been shutdown
    /// </summary>
    Shutdown
}