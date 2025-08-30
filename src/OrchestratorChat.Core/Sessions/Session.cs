using OrchestratorChat.Core.Messages;

namespace OrchestratorChat.Core.Sessions;

/// <summary>
/// Represents a chat session between users and agents
/// </summary>
public class Session
{
    /// <summary>
    /// Unique identifier for the session
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();
    
    /// <summary>
    /// Display name for the session
    /// </summary>
    public string Name { get; set; }
    
    /// <summary>
    /// Type of session
    /// </summary>
    public SessionType Type { get; set; }
    
    /// <summary>
    /// Current status of the session
    /// </summary>
    public SessionStatus Status { get; set; }
    
    /// <summary>
    /// Timestamp when the session was created
    /// </summary>
    public DateTime CreatedAt { get; set; }
    
    /// <summary>
    /// Timestamp of the last activity in this session
    /// </summary>
    public DateTime LastActivityAt { get; set; }
    
    /// <summary>
    /// Alias for LastActivityAt for compatibility
    /// </summary>
    public DateTime LastActivity
    {
        get => LastActivityAt;
        set => LastActivityAt = value;
    }
    
    /// <summary>
    /// List of agent IDs participating in this session
    /// </summary>
    public List<string> ParticipantAgentIds { get; set; } = new();
    
    /// <summary>
    /// List of agent info objects participating in this session
    /// </summary>
    public List<OrchestratorChat.Core.Agents.AgentInfo> ParticipantAgents { get; set; } = new();
    
    /// <summary>
    /// List of messages in this session
    /// </summary>
    public List<AgentMessage> Messages { get; set; } = new();
    
    /// <summary>
    /// Context data shared across the session
    /// </summary>
    public Dictionary<string, object> Context { get; set; } = new();
    
    /// <summary>
    /// Working directory for this session
    /// </summary>
    public string WorkingDirectory { get; set; }
    
    /// <summary>
    /// Project ID if this session is associated with a specific project
    /// </summary>
    public string ProjectId { get; set; }
}