namespace OrchestratorChat.Core.Models;

/// <summary>
/// Summary information about a session
/// </summary>
public class SessionSummary
{
    /// <summary>
    /// Unique identifier for the session
    /// </summary>
    public string Id { get; set; } = string.Empty;
    
    /// <summary>
    /// Display name for the session
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Timestamp when the session was created
    /// </summary>
    public DateTime CreatedAt { get; set; }
    
    /// <summary>
    /// Timestamp of the last activity in this session
    /// </summary>
    public DateTime LastActivityAt { get; set; }
    
    /// <summary>
    /// Number of messages in the session
    /// </summary>
    public int MessageCount { get; set; }
    
    /// <summary>
    /// Content of the last message in the session
    /// </summary>
    public string? LastMessage { get; set; }
}