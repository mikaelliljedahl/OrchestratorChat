namespace OrchestratorChat.Core.Sessions;

/// <summary>
/// Defines the type of session
/// </summary>
public enum SessionType
{
    /// <summary>
    /// Session with a single agent
    /// </summary>
    SingleAgent,
    
    /// <summary>
    /// Session with multiple agents
    /// </summary>
    MultiAgent,
    
    /// <summary>
    /// Session managed by an orchestrator
    /// </summary>
    Orchestrated
}