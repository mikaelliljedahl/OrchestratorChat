namespace OrchestratorChat.Core.Agents;

/// <summary>
/// Defines the types of agents available in the system
/// </summary>
public enum AgentType
{
    /// <summary>
    /// Claude agent type
    /// </summary>
    Claude,
    
    /// <summary>
    /// Saturn agent type
    /// </summary>
    Saturn,
    
    /// <summary>
    /// GPT-4 based agent type
    /// </summary>
    GPT4,
    
    /// <summary>
    /// Custom agent type
    /// </summary>
    Custom,
    
    /// <summary>
    /// Orchestrator agent type
    /// </summary>
    Orchestrator
}