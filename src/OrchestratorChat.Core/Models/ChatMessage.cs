using OrchestratorChat.Core.Messages;

namespace OrchestratorChat.Core.Models;

/// <summary>
/// Represents a chat message in a session
/// </summary>
public class ChatMessage
{
    /// <summary>
    /// Unique identifier for the message
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();
    
    /// <summary>
    /// Content of the message
    /// </summary>
    public string Content { get; set; } = string.Empty;
    
    /// <summary>
    /// Role of the message sender
    /// </summary>
    public MessageRole Role { get; set; }
    
    /// <summary>
    /// ID of the session this message belongs to
    /// </summary>
    public string SessionId { get; set; } = string.Empty;
    
    /// <summary>
    /// Timestamp when the message was created
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// ID of the sender (for tracking message origin)
    /// </summary>
    public string? SenderId { get; set; }
    
    /// <summary>
    /// Additional metadata associated with the message
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}