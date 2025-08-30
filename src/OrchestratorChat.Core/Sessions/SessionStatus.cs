namespace OrchestratorChat.Core.Sessions;

/// <summary>
/// Defines the current status of a session
/// </summary>
public enum SessionStatus
{
    /// <summary>
    /// Session is currently active
    /// </summary>
    Active,
    
    /// <summary>
    /// Session is paused
    /// </summary>
    Paused,
    
    /// <summary>
    /// Session has completed successfully
    /// </summary>
    Completed,
    
    /// <summary>
    /// Session has failed or encountered an error
    /// </summary>
    Failed
}