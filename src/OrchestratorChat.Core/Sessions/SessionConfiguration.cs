namespace OrchestratorChat.Core.Sessions;

/// <summary>
/// Configuration settings for creating a new session
/// </summary>
public class SessionConfiguration
{
    /// <summary>
    /// Display name for the session
    /// </summary>
    public string Name { get; set; }
    
    /// <summary>
    /// Type of session to create
    /// </summary>
    public SessionType Type { get; set; }
    
    /// <summary>
    /// List of agent IDs that will participate in this session
    /// </summary>
    public List<string> AgentIds { get; set; } = new();
    
    /// <summary>
    /// Working directory for the session
    /// </summary>
    public string WorkingDirectory { get; set; }
    
    /// <summary>
    /// Whether to persist message history to storage
    /// </summary>
    public bool PersistHistory { get; set; } = true;
    
    /// <summary>
    /// Maximum number of messages to keep in memory
    /// </summary>
    public int MaxMessages { get; set; } = 1000;
    
    /// <summary>
    /// Initial context data for the session
    /// </summary>
    public Dictionary<string, object> InitialContext { get; set; } = new();
}