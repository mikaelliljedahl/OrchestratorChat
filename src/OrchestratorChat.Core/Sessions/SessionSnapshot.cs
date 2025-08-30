namespace OrchestratorChat.Core.Sessions;

/// <summary>
/// Represents a snapshot of a session at a specific point in time
/// </summary>
public class SessionSnapshot
{
    /// <summary>
    /// Unique identifier for the snapshot
    /// </summary>
    public string Id { get; set; }
    
    /// <summary>
    /// ID of the session this snapshot belongs to
    /// </summary>
    public string SessionId { get; set; }
    
    /// <summary>
    /// Timestamp when the snapshot was created
    /// </summary>
    public DateTime CreatedAt { get; set; }
    
    /// <summary>
    /// Human-readable description of the snapshot
    /// </summary>
    public string Description { get; set; }
    
    /// <summary>
    /// Complete state of the session at the time of the snapshot
    /// </summary>
    public Session SessionState { get; set; }
    
    /// <summary>
    /// State of all agents in the session at the time of the snapshot
    /// </summary>
    public Dictionary<string, AgentState> AgentStates { get; set; } = new();
}